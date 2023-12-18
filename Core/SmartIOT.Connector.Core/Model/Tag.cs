using SmartIOT.Connector.Core.Conf;
using System.Net.Http.Headers;

namespace SmartIOT.Connector.Core.Model;

public class Tag
{
    public TagConfiguration TagConfiguration { get; }
    public string TagId => TagConfiguration.TagId;
    public TagType TagType => TagConfiguration.TagType;
    public int ByteOffset => TagConfiguration.ByteOffset;
    public int Size => TagConfiguration.Size;
    public bool IsWriteSynchronizationRequested { get; set; }
    public int Weight => TagConfiguration.Weight;

    public bool IsInitialized { get; internal set; }
    public int ErrorCode { get; internal set; }
    public int ErrorCount { get; internal set; }
    public DateTime LastErrorDateTime { get; internal set; }
    public TimeSpan PartialReadTotalTime { get; internal set; }
    public TimeSpan PartialReadMeanTime { get; internal set; }
    public long PartialReadCount { get; internal set; }
    public long SynchronizationCount { get; internal set; }
    public TimeSpan SynchronizationAvgTime { get; internal set; }
    public int WritesCount { get; internal set; }

    internal byte[] Data { get; }
    internal byte[] OldData { get; }
    internal int CurrentReadIndex { get; set; }
    internal DateTime LastDeviceSynchronization { get; set; }
    internal int Points { get; set; }

    public Tag(TagConfiguration tagConfiguration)
    {
        TagConfiguration = tagConfiguration;

        Data = new byte[Size];
        OldData = new byte[Size];

        IsInitialized = false;
    }

#pragma warning disable S2551 // Shared resources should not be used for locking: we purposefully lock on "this" to avoid races between tag-scheduler and services that request tag write.

    /// <summary>
    /// This method copies the data received as an argument to the specified startOffset.
    /// The startOffset must be passed as an absolute value, so if a tag starts at byte 100
    /// and the method intends to write bytes from 110 to 120, it will pass startOffset = 110 and an array of length = 11 as arguments.
    /// The method performs a range check to determine the intersection of the passed range, compared to the definition
    /// of the tag. If there is no intersection, false is returned and nothing is copied.
    /// If there at least 1 byte of intersection, that range is copied, flag <see cref="Tag.IsWriteSynchronizationRequested"/> is set and true is returned.
    /// Be aware the flag IsWriteSynchronizationRequested is set even if the underlying data is not changed: no byte compare is performed to check for modifications.
    /// </summary>
    public bool TryMergeData(ReadOnlySpan<byte> data, int startOffset, int size)
    {
        if (startOffset + size > ByteOffset && startOffset < ByteOffset + Size)
        {
            int start = Math.Max(startOffset, ByteOffset);
            int end = Math.Min(startOffset + size, ByteOffset + Data.Length);

            lock (this)
            {
                data.Slice(start - startOffset, end - start).CopyTo(Data.AsSpan().Slice(start - ByteOffset));
                IsWriteSynchronizationRequested = true;
            }

            return true;
        }

        return false;
    }

    public bool TryMergeData(ReadOnlySpan<byte> data, int startOffset)
    {
        return TryMergeData(data, startOffset, data.Length);
    }

#pragma warning restore S2551 // Shared resources should not be used for locking

    /// <summary>
    /// This method returns a copy of the current bytes stored in the tag
    /// </summary>
    public byte[] GetData()
    {
        var bytes = new byte[Data.Length];

        Array.Copy(Data, bytes, bytes.Length);

        return bytes;
    }
}
