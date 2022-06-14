using ProtoBuf;
using System.Text;

namespace SmartIOT.Connector.Messages.Serializers
{
	public class ProtobufStreamMessageSerializer : IStreamMessageSerializer
	{
		public object? DeserializeMessage(Stream stream)
		{
			try
			{
				byte typeValue;
				using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, true))
				{
					typeValue = reader.ReadByte();
				}

				return typeValue switch
				{
					1 => DeserializeMessage<TagEvent>(stream),
					2 => DeserializeMessage<DeviceEvent>(stream),
					3 => DeserializeMessage<TagWriteRequestCommand>(stream),
					_ => throw new InvalidDataException($"Message type {typeValue} is not recognized"),
				};
			}
			catch (EndOfStreamException)
			{
				return null;
			}
		}

		private T DeserializeMessage<T>(Stream stream)
		{
			return Serializer.DeserializeWithLengthPrefix<T>(stream, PrefixStyle.Base128);
		}

		public void SerializeMessage(Stream stream, object message)
		{
			switch (message)
			{
				case TagEvent t:
					SerializeMessage(1, stream, t);
					break;
				case DeviceEvent d:
					SerializeMessage(2, stream, d);
					break;
				case TagWriteRequestCommand c:
					SerializeMessage(3, stream, c);
					break;
				default:
					throw new ArgumentException($"Message type {message.GetType().FullName} is not managed", nameof(message));
			}
		}

		private void SerializeMessage<T>(byte typeValue, Stream stream, T message)
		{
			using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, true))
			{
				// serialize the type
				stream.WriteByte(typeValue);
			}

			// serialize length + payload
			Serializer.SerializeWithLengthPrefix<T>(stream, message, PrefixStyle.Base128);
		}
	}
}
