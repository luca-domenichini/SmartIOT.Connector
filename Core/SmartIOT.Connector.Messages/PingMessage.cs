using ProtoBuf;

namespace SmartIOT.Connector.Messages
{
    /// <summary>
    /// This class is used to send a keepalive message to the external system
    /// </summary>
    [ProtoContract]
    public class PingMessage
    {
    }
}
