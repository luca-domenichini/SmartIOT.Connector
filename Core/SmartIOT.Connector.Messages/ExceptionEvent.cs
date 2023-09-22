using ProtoBuf;

namespace SmartIOT.Connector.Messages;

[ProtoContract]
public class ExceptionEvent
{
    [ProtoMember(1)]
    public string Message { get; set; } = string.Empty;

    [ProtoMember(2)]
    public string Exception { get; set; } = string.Empty;

    public ExceptionEvent()
    {
    }

    public ExceptionEvent(string message, string exception)
    {
        Message = message;
        Exception = exception;
    }
}
