using System.Text.Json;

namespace SmartIOT.Connector.Messages
{
	public class JsonMessageSerializer : IMessageSerializer
	{
		private readonly JsonSerializerOptions _options;

		public JsonMessageSerializer()
			: this(CreateDefaultSerializerOptions())
		{

		}

		private static JsonSerializerOptions CreateDefaultSerializerOptions()
		{
			var options = new JsonSerializerOptions()
			{
				ReadCommentHandling = JsonCommentHandling.Skip
			};
			return options;
		}

		public JsonMessageSerializer(JsonSerializerOptions options)
		{
			_options = options;
		}

		public byte[] SerializeMessage(object message)
		{
			return JsonSerializer.SerializeToUtf8Bytes(message, _options);
		}

		public T? DeserializeMessage<T>(byte[] message)
		{
			return JsonSerializer.Deserialize<T>(message, _options);
		}
	}
}
