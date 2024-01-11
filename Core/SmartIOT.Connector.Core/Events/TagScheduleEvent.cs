using SmartIOT.Connector.Core.Model;

namespace SmartIOT.Connector.Core.Events;

public class TagScheduleEvent
{
    public Device Device { get; }
    public Tag Tag { get; }
    public int StartOffset { get; }
    public byte[]? Data { get; }
    public int ErrorNumber { get; }
    public string? Description { get; }
    public bool IsErrorNumberChanged { get; set; }

    public static TagScheduleEvent BuildTagData(Device device, Tag tag, int startOffset, byte[] data, bool isErrorNumberChanged)
    {
        return new TagScheduleEvent(device, tag, startOffset, data, isErrorNumberChanged);
    }

    public static TagScheduleEvent BuildTagData(Device device, Tag tag, bool isErrorNumberChanged)
    {
        lock (tag)
        {
            byte[] data = new byte[tag.Data.Length];
            Array.Copy(tag.Data, 0, data, 0, data.Length);
            return new TagScheduleEvent(device, tag, tag.ByteOffset, data, isErrorNumberChanged);
        }
    }

    public static TagScheduleEvent BuildTagData(Device device, Tag tag, int startOffset, int length, bool isErrorNumberChanged)
    {
        lock (tag)
        {
            byte[] data = new byte[length];
            Array.Copy(tag.Data, startOffset - tag.ByteOffset, data, 0, data.Length);
            return new TagScheduleEvent(device, tag, startOffset, data, isErrorNumberChanged);
        }
    }

    public static TagScheduleEvent BuildEmptyTagData(Device device, Tag tag, bool isErrorNumberChanged)
    {
        return new TagScheduleEvent(device, tag, tag.ByteOffset, Array.Empty<byte>(), isErrorNumberChanged);
    }

    public static TagScheduleEvent BuildTagStatus(Device device, Tag tag, int errorNumber, string description, bool isErrorNumberChanged)
    {
        return new TagScheduleEvent(device, tag, errorNumber, description, isErrorNumberChanged);
    }

    private TagScheduleEvent(Device device, Tag tag, int errorNumber, string description, bool isErrorNumberChanged)
    {
        Device = device;
        Tag = tag;
        ErrorNumber = errorNumber;
        Description = description;
        IsErrorNumberChanged = isErrorNumberChanged;
    }

    private TagScheduleEvent(Device device, Tag tag, int startOffset, byte[] data, bool isErrorNumberChanged)
    {
        Device = device;
        Tag = tag;
        StartOffset = startOffset;
        ErrorNumber = 0;
        Data = data;
        IsErrorNumberChanged = isErrorNumberChanged;
    }
}
