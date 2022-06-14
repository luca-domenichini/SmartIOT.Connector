using ProtoBuf;

namespace SmartIOT.Connector.Messages.Serializers
{
	public class ProtobufSingleMessageSerializer : ISingleMessageSerializer
	{
		public T? DeserializeMessage<T>(byte[] bytes)
		{
			return (T?)Serializer.Deserialize(typeof(T), new MemoryStream(bytes));
		}

		public byte[] SerializeMessage(object message)
		{
			var stream = new MemoryStream();
			Serializer.Serialize(stream, message);

			return stream.ToArray();
		}
	}
}
