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

    public Device(DeviceConfiguration deviceConfiguration)
    {
        Configuration = deviceConfiguration;
        DeviceStatus = deviceConfiguration.Enabled ? DeviceStatus.UNINITIALIZED : DeviceStatus.DISABLED;

        foreach (var tagConfiguration in deviceConfiguration.Tags)
        {
            InternalAddTag(tagConfiguration);
        }
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
        var tag = new Tag(tagConfiguration);
        _tags.Add(tag);

        switch (tag.TagType)
        {
            case TagType.READ:
                _weightMCMRead = CalculateMCM(TagType.READ);
                break;

            case TagType.WRITE:
                _weightMCMWrite = CalculateMCM(TagType.WRITE);
                break;
        }
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

            switch (tag.TagType)
            {
                case TagType.READ:
                    _weightMCMRead = CalculateMCM(TagType.READ);
                    break;

                case TagType.WRITE:
                    _weightMCMWrite = CalculateMCM(TagType.WRITE);
                    break;
            }
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

            switch (tag.TagType)
            {
                case TagType.READ:
                    _weightMCMRead = CalculateMCM(TagType.READ);
                    break;

                case TagType.WRITE:
                    _weightMCMWrite = CalculateMCM(TagType.WRITE);
                    break;
            }

            return true;
        }
    }

    internal void IncrementOrReseDeviceErrorCode(int err)
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

    internal void SetTagSynchronized(Tag tag, DateTime instant)
    {
        tag.LastDeviceSynchronization = instant;

        TagType type = tag.TagType;

        if (type == TagType.READ)
        {
            tag.Points += tag.Weight;

            int minPunti = GetMinPoints(type);
            while (minPunti > 0 && minPunti % GetWeightMCM(type) == 0)
            {
                foreach (Tag t in _tags)
                {
                    if (t.TagType == type)
                    {
                        t.Points -= GetWeightMCM(type);
                        if (t.Points < 0)
                            t.Points = 0;
                    }
                }
                minPunti = GetMinPoints(type);
            }

            bool riscalaturaPossibile = true;
            int maxPunti = int.MaxValue;

            while (riscalaturaPossibile && maxPunti > 0)
            {
                maxPunti = GetMaxPoints(type);
                List<Tag> list = GetTagsInRange(type, maxPunti - GetWeightMCM(type), maxPunti);

                int min = int.MaxValue;
                foreach (Tag t in list)
                {
                    if (t.Points < min)
                        min = t.Points;
                }

                if (min - GetWeightMCM(type) < 0)
                {
                    riscalaturaPossibile = false;
                    continue;
                }

                list = GetTagsInRange(type, min - GetWeightMCM(type), min);

                foreach (Tag t in list)
                {
                    if (t.Points != min)
                    {
                        riscalaturaPossibile = false;
                        break;
                    }
                }

                if (riscalaturaPossibile)
                {
                    foreach (Tag t in _tags)
                    {
                        if (t.Points >= min)
                        {
                            t.Points -= GetWeightMCM(type);
                        }
                        if (t.Points < 0)
                            t.Points = 0;
                    }
                }
            }
        }
    }

    private List<Tag> GetTagsInRange(TagType type, int minPoints, int maxPoints)
    {
        var list = new List<Tag>();
        foreach (Tag t in _tags)
        {
            if (t.TagType == type && t.Points >= minPoints && t.Points <= maxPoints)
                list.Add(t);
        }
        return list;
    }

    private int GetMaxPoints(TagType type)
    {
        int max = int.MinValue;
        foreach (Tag t in _tags)
        {
            if (t.TagType == type && t.Points > max)
                max = t.Points;
        }
        return max;
    }

    private int GetMinPoints(TagType type)
    {
        int min = int.MaxValue;
        foreach (Tag t in _tags)
        {
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
        int max = int.MinValue;

        foreach (Tag t in _tags)
        {
            if (t.TagType == type && t.Weight > max)
                max = t.Weight;
        }

        bool mcmTrovato = false;
        while (!mcmTrovato)
        {
            mcmTrovato = true;

            foreach (Tag t in _tags)
            {
                if (t.TagType == type && max % t.Weight != 0)
                {
                    mcmTrovato = false;
                    break;
                }
            }

            if (mcmTrovato)
                break;

            max++;
        }

        return max;
    }
}
