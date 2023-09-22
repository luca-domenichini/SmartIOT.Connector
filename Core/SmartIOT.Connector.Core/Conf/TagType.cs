using System.Text.Json.Serialization;

namespace SmartIOT.Connector.Core.Conf
{
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum TagType
    {
        READ,
        WRITE
    }
}
