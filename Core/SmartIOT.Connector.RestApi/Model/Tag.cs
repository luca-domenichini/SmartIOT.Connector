using SmartIOT.Connector.Core.Conf;

namespace SmartIOT.Connector.RestApi.Model;

public class Tag
{
    public string TagId { get; }
    public TagType TagType { get; }
    public int ByteOffset { get; }
    public int Size { get; }
    public int Weight { get; }
    public bool IsInitialized { get; }
    public int ErrorCode { get; }
    public int ErrorCount { get; }
    public DateTime? LastErrorDateTime { get; }
    public TimeSpan PartialReadTotalTime { get; }
    public TimeSpan PartialReadMeanTime { get; }
    public long PartialReadCount { get; }
    public long SynchronizationCount { get; }
    public TimeSpan SynchronizationAvgTime { get; }
    public int WritesCount { get; }

    public Tag(string tagId, TagType tagType, int byteOffset, int size, int weight, bool isInitialized, int errorCode, int errorCount, long lastErrorTicks, TimeSpan partialReadTotalTime, TimeSpan partialReadMeanTime, long partialReadCount, long synchronizationCount, TimeSpan synchronizationAvgTime, int writesCount)
    {
        TagId = tagId;
        TagType = tagType;
        ByteOffset = byteOffset;
        Size = size;
        Weight = weight;
        IsInitialized = isInitialized;
        ErrorCode = errorCode;
        ErrorCount = errorCount;
        LastErrorDateTime = ToLocalDateTime(lastErrorTicks);
        PartialReadTotalTime = partialReadTotalTime;
        PartialReadMeanTime = partialReadMeanTime;
        PartialReadCount = partialReadCount;
        SynchronizationCount = synchronizationCount;
        SynchronizationAvgTime = synchronizationAvgTime;
        WritesCount = writesCount;
    }

    public Tag(Core.Model.Tag tag)
    {
        TagId = tag.TagId;
        TagType = tag.TagType;
        ByteOffset = tag.ByteOffset;
        Size = tag.Size;
        Weight = tag.Weight;
        IsInitialized = tag.IsInitialized;
        ErrorCode = tag.ErrorCode;
        ErrorCount = tag.ErrorCount;
        LastErrorDateTime = ToLocalDateTime(tag.LastErrorTicks);
        PartialReadTotalTime = tag.PartialReadTotalTime;
        PartialReadMeanTime = tag.PartialReadMeanTime;
        PartialReadCount = tag.PartialReadCount;
        SynchronizationCount = tag.SynchronizationCount;
        SynchronizationAvgTime = tag.SynchronizationAvgTime;
        WritesCount = tag.WritesCount;
    }

    /// <summary>
    /// The core model stores instants as local wall-clock ticks, so they are converted back to a
    /// local DateTime here: 0 means "never errored" and maps to null.
    /// </summary>
    private static DateTime? ToLocalDateTime(long ticks)
    {
        return ticks == 0 ? null : new DateTime(ticks, DateTimeKind.Local);
    }
}
