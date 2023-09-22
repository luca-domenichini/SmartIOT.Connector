namespace SmartIOT.Connector.Messages.Serializers;

public interface ISingleMessageSerializer
{
    public byte[] SerializeMessage(object message);

    public T? DeserializeMessage<T>(byte[] bytes);
}
