using System.Text.Json;

namespace SmartIOT.Connector.Messages.Serializers;

public class JsonSingleMessageSerializer : ISingleMessageSerializer
{
    private readonly JsonSerializerOptions _options;

    public JsonSingleMessageSerializer()
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

    public JsonSingleMessageSerializer(JsonSerializerOptions options)
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
