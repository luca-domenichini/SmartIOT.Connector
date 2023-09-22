namespace SmartIOT.Connector.Core;

public class DeviceDriverException : Exception
{
    public DeviceDriverException(string? message) : base(message)
    {
    }

    public DeviceDriverException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
