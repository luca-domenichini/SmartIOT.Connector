using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace SmartIOT.Connector.Messages.Serializers
{
	public class JsonStreamMessageSerializer : IStreamMessageSerializer
	{
		private readonly JsonSerializerOptions _options;

		private static JsonSerializerOptions CreateDefaultSerializerOptions()
		{
			return new JsonSerializerOptions()
			{
				ReadCommentHandling = JsonCommentHandling.Skip
			};
		}

		public JsonStreamMessageSerializer()
			: this(CreateDefaultSerializerOptions())
		{

		}
		public JsonStreamMessageSerializer(JsonSerializerOptions options)
		{
			_options = options;
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
				case PingMessage p:
					SerializeMessage(99, stream, p);
					break;
				default:
					throw new ArgumentException($"Message type {message.GetType().FullName} is not managed", nameof(message));
			}
		}

		private void SerializeMessage<T>(byte typeValue, Stream stream, T message)
		{
			// serialize the type
			stream.WriteByte(typeValue);

			// serialize payload
			JsonSerializer.Serialize(stream, message, _options);

			// serialize newline \n
			stream.WriteByte(0x0A);
		}

		public object? DeserializeMessage(Stream stream)
		{
			int typeValue = stream.ReadByte();
			if (typeValue == -1)
				return null;

			return typeValue switch
			{
				1 => DeserializeMessage<TagEvent>(stream),
				2 => DeserializeMessage<DeviceEvent>(stream),
				3 => DeserializeMessage<TagWriteRequestCommand>(stream),
				99 => DeserializeMessage<PingMessage>(stream),
				_ => throw new InvalidDataException($"Message type {typeValue} is not recognized"),
			};
		}

		private T? DeserializeMessage<T>(Stream stream)
		{
			string? line = ReadLine(stream);
			if (line == null)
				return default;

			return JsonSerializer.Deserialize<T>(line, _options);
		}

		private string? ReadLine(Stream stream)
		{
			var bytes = new List<byte>();
			int current;

			while ((current = stream.ReadByte()) != -1 && current != 0x0A)
			{
				bytes.Add((byte)current);
			}

			if (bytes.Count > 0)
				return Encoding.UTF8.GetString(bytes.ToArray());

			return null;
		}
	}
}
