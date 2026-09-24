using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Util;

namespace SmartIOT.Connector.Core.Model;

public class Device
{
    public DeviceConfiguration Configuration { get; }

    public DeviceStatus DeviceStatus { get; internal set; }
    public string Name => Configuration.Name;
    public string DeviceId => Configuration.DeviceId;
    public int ErrorCode { get; internal set; }
    public int ErrorCount { get; internal set; }
    public IReadOnlyList<Tag> Tags => _tags;

    public bool IsPartialReadsEnabled => Configuration.IsPartialReadsEnabled;
    public int SinglePDUReadBytes { get; set; }
    public bool IsWriteOptimizationEnabled => Configuration.IsWriteOptimizationEnabled;
    public int SinglePDUWriteBytes { get; set; }
    public int PDULength { get; set; }

    private readonly CopyOnWriteArrayList<Tag> _tags = new CopyOnWriteArrayList<Tag>();

    private int _weightMCMRead;
    private int _weightMCMWrite;

    /// <summary>
    /// Number of READ tags currently configured. Read on every READ tag synchronisation to take a
    /// cheap fast path when there is nothing to rebalance, so it must be kept in sync with
    /// <see cref="_tags"/> by <see cref="RecalculateDerivedValues"/>.
    /// </summary>
    private int _readTagCount;

    public Device(DeviceConfiguration deviceConfiguration)
    {
        Configuration = deviceConfiguration;
        DeviceStatus = deviceConfiguration.Enabled ? DeviceStatus.UNINITIALIZED : DeviceStatus.DISABLED;

        foreach (var tagConfiguration in deviceConfiguration.Tags)
        {
            _tags.Add(new Tag(tagConfiguration));
        }

        // derived values are computed once, after the whole tag list has been built.
        RecalculateDerivedValues();
    }

    public bool IsEnabled()
    {
        return DeviceStatus != DeviceStatus.DISABLED;
    }

    public void SetEnabled(bool enabled)
    {
        bool wasEnabled = DeviceStatus != DeviceStatus.DISABLED;

        if (enabled != wasEnabled)
        {
            if (DeviceStatus == DeviceStatus.DISABLED)
            {
                DeviceStatus = DeviceStatus.UNINITIALIZED;
            }
            else
            {
                DeviceStatus = DeviceStatus.DISABLED;
            }

            Configuration.Enabled = enabled;
        }
    }

    private void InternalAddTag(TagConfiguration tagConfiguration)
    {
        _tags.Add(new Tag(tagConfiguration));
        RecalculateDerivedValues();
    }

    /// <summary>
    /// Recomputes the values derived from the tag list (the READ tag count and the least common
    /// multiple of the tag weights, per tag type). Must be called after every structural change
    /// of the tag list. Configuration-time only: never called from the scheduling hot path.
    /// </summary>
    private void RecalculateDerivedValues()
    {
        Tag[] tags = _tags.GetSnapshot();

        int readCount = 0;
        foreach (Tag tag in tags)
        {
            if (tag.TagType == TagType.READ)
                readCount++;
        }

        _readTagCount = readCount;
        // recomputed for both types: a tag update may change the tag type, leaving the
        // weight of the previous type stale if only one of the two were recalculated
        _weightMCMRead = CalculateMCM(TagType.READ);
        _weightMCMWrite = CalculateMCM(TagType.WRITE);
    }

    public void AddTag(TagConfiguration tagConfiguration)
    {
        lock (_tags)
        {
            Configuration.Tags.Add(tagConfiguration);
            InternalAddTag(tagConfiguration);
        }
    }

    public void RemoveTag(Tag tag)
    {
        lock (_tags)
        {
            var tc = Configuration.Tags.FirstOrDefault(x => x.TagId.Equals(tag.TagId, StringComparison.InvariantCultureIgnoreCase));
            if (tc != null)
                Configuration.Tags.Remove(tc);

            _tags.Remove(tag);

            RecalculateDerivedValues();
        }
    }

    public bool UpdateTag(TagConfiguration tagConfiguration)
    {
        lock (_tags)
        {
            var index = Configuration.Tags.Where(x => x.TagId.Equals(tagConfiguration.TagId, StringComparison.InvariantCultureIgnoreCase))
                .Select((tag, index) => index)
                .FirstOrDefault(-1);

            if (index < 0)
                return false;

            Configuration.Tags[index] = tagConfiguration;

            var old = _tags.First(x => x.TagId.Equals(tagConfiguration.TagId, StringComparison.InvariantCultureIgnoreCase));

            var tag = new Tag(tagConfiguration);
            _tags.Replace(old, tag);

            RecalculateDerivedValues();

            return true;
        }
    }

    internal void IncrementOrResetDeviceErrorCode(int err)
    {
        ErrorCode = err;
        if (err != 0)
        {
            ErrorCount++;
            DeviceStatus = DeviceStatus.ERROR;
        }
        else
        {
            ErrorCount = 0;
            DeviceStatus = DeviceStatus.OK;
        }
    }

    internal void SetTagSynchronized(Tag tag, long instant)
    {
        tag.LastDeviceSynchronization = instant;

        if (tag.TagType != TagType.READ)
            return;

        tag.Points += tag.Weight;

        // Fast path: with at most one READ tag there is nothing to rebalance. Points only ever
        // change by +/- weightMCM starting from 0, so the loops below would just walk the only
        // READ tag back to 0 anyway - skip the whole O(number of tags) redistribution.
        if (_readTagCount <= 1)
        {
            tag.Points = 0;
            return;
        }

        // snapshot of the tag list: iterated by index, so no enumerator is allocated per pass
        Tag[] tags = _tags.GetSnapshot();
        TagType type = TagType.READ;
        int weightMCM = GetWeightMCM(type);

        int minPunti = GetMinPoints(tags, type);
        while (minPunti > 0 && minPunti % weightMCM == 0)
        {
            for (int i = 0; i < tags.Length; i++)
            {
                Tag t = tags[i];
                if (t.TagType == type)
                {
                    t.Points -= weightMCM;
                    if (t.Points < 0)
                        t.Points = 0;
                }
            }
            minPunti = GetMinPoints(tags, type);
        }

        bool riscalaturaPossibile = true;
        int maxPunti = int.MaxValue;

        while (riscalaturaPossibile && maxPunti > 0)
        {
            maxPunti = GetMaxPoints(tags, type);

            // minimum points among the tags sitting in [maxPunti - weightMCM, maxPunti],
            // computed in place instead of materialising a filtered list per iteration
            int min = int.MaxValue;
            for (int i = 0; i < tags.Length; i++)
            {
                Tag t = tags[i];
                if (t.TagType == type && t.Points >= maxPunti - weightMCM && t.Points <= maxPunti && t.Points < min)
                    min = t.Points;
            }

            if (min - weightMCM < 0)
            {
                riscalaturaPossibile = false;
                continue;
            }

            // rebalance is possible only if every tag in [min - weightMCM, min] sits exactly on "min"
            riscalaturaPossibile = true;
            for (int i = 0; i < tags.Length; i++)
            {
                Tag t = tags[i];
                if (t.TagType == type && t.Points >= min - weightMCM && t.Points < min)
                {
                    riscalaturaPossibile = false;
                    break;
                }
            }

            if (riscalaturaPossibile)
            {
                // note: as in the original implementation this correction is applied to every tag,
                // regardless of its type
                for (int i = 0; i < tags.Length; i++)
                {
                    Tag t = tags[i];
                    if (t.Points >= min)
                        t.Points -= weightMCM;
                    if (t.Points < 0)
                        t.Points = 0;
                }
            }
        }
    }

    private static int GetMaxPoints(Tag[] tags, TagType type)
    {
        int max = int.MinValue;
        for (int i = 0; i < tags.Length; i++)
        {
            Tag t = tags[i];
            if (t.TagType == type && t.Points > max)
                max = t.Points;
        }
        return max;
    }

    private static int GetMinPoints(Tag[] tags, TagType type)
    {
        int min = int.MaxValue;
        for (int i = 0; i < tags.Length; i++)
        {
            Tag t = tags[i];
            if (t.TagType == type && t.Points < min)
                min = t.Points;
        }
        return min;
    }

    private int GetWeightMCM(TagType type)
    {
        if (type == TagType.READ)
            return _weightMCMRead;
        else
            return _weightMCMWrite;
    }

    private int CalculateMCM(TagType type)
    {
        return FindLCM(_tags.Where(x => x.TagType == type).Select(x => x.Weight));
    }

    private static int FindLCM(IEnumerable<int> numbers)
    {
        int lcm = int.MinValue;

        foreach (var n in numbers)
        {
            if (lcm == int.MinValue)
                lcm = n;
            else
                lcm = Math.Abs(lcm * n) / FindGCD(lcm, n);
        }

        return lcm;
    }

    private static int FindGCD(int a, int b)
    {
        while (b != 0)
        {
            int temp = b;
            b = a % b;
            a = temp;
        }

        return a;
    }
}
