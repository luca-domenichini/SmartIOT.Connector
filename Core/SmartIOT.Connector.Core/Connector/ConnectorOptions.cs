namespace SmartIOT.Connector.Core.Connector;

public class ConnectorOptions
{
    public string ConnectionString { get; }
    public bool IsPublishWriteEvents { get; }

    public ConnectorOptions(string connectionString, bool isPublishWriteEvents)
    {
        ConnectionString = connectionString;
        IsPublishWriteEvents = isPublishWriteEvents;
    }
}
