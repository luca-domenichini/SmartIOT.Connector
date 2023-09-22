using ProtoBuf;

namespace SmartIOT.Connector.Messages
{
    [ProtoContract]
    public class TagWriteRequestCommand
    {
        [ProtoMember(1)]
        public string DeviceId { get; set; } = string.Empty;

        [ProtoMember(2)]
        public string TagId { get; set; } = string.Empty;

        [ProtoMember(3)]
        public int StartOffset { get; set; }

        [ProtoMember(4)]
        public byte[] Data { get; set; } = Array.Empty<byte>();

        public TagWriteRequestCommand()
        {
        }

        public TagWriteRequestCommand(string deviceId, string tagId, int startOffset, byte[] data)
        {
            DeviceId = deviceId;
            TagId = tagId;
            StartOffset = startOffset;
            Data = data;
        }

        public override string? ToString()
        {
            return $"[{nameof(TagWriteRequestCommand)}] Device {DeviceId}, Tag {TagId}, StartOffset {StartOffset}, Data[{Data.Length}]";
        }
    }
}