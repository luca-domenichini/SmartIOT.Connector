using ProtoBuf;

namespace SmartIOT.Connector.Messages.Serializers;

public class ProtobufSingleMessageSerializer : ISingleMessageSerializer
{
    public T? DeserializeMessage<T>(ReadOnlyMemory<byte> bytes)
    {
        return Serializer.Deserialize<T>(bytes);
    }

    public byte[] SerializeMessage(object message)
    {
        using var stream = new MemoryStream();
        Serializer.Serialize(stream, message);

        return stream.ToArray();
    }
}
