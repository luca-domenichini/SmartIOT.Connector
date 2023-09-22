namespace SmartIOT.Connector.Messages.Serializers
{
    public interface IStreamMessageSerializer
    {
        public void SerializeMessage(Stream stream, object message);

        public object? DeserializeMessage(Stream stream);
    }
}
