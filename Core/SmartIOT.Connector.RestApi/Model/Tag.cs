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

    public Tag(string tagId, TagType tagType, int byteOffset, int size, int weight, bool isInitialized, int errorCode, int errorCount, DateTime lastErrorDateTime, TimeSpan partialReadTotalTime, TimeSpan partialReadMeanTime, long partialReadCount, long synchronizationCount, TimeSpan synchronizationAvgTime, int writesCount)
    {
        TagId = tagId;
        TagType = tagType;
        ByteOffset = byteOffset;
        Size = size;
        Weight = weight;
        IsInitialized = isInitialized;
        ErrorCode = errorCode;
        ErrorCount = errorCount;
        LastErrorDateTime = lastErrorDateTime == DateTime.MinValue ? null : lastErrorDateTime;
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
        LastErrorDateTime = tag.LastErrorDateTime == DateTime.MinValue ? null : tag.LastErrorDateTime;
        PartialReadTotalTime = tag.PartialReadTotalTime;
        PartialReadMeanTime = tag.PartialReadMeanTime;
        PartialReadCount = tag.PartialReadCount;
        SynchronizationCount = tag.SynchronizationCount;
        SynchronizationAvgTime = tag.SynchronizationAvgTime;
        WritesCount = tag.WritesCount;
    }
}
