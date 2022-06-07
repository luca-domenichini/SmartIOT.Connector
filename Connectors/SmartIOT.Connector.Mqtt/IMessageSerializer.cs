namespace SmartIOT.Connector.Mqtt
{
	public interface IMessageSerializer
	{
		public byte[] SerializeMessage(object message);
		public T? DeserializeMessage<T>(byte[] bytes);
	}
}
