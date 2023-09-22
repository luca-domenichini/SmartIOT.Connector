namespace SmartIOT.Connector.Core.Events;

public class ConnectorStartedEventArgs : EventArgs
{
    public IConnector Connector { get; }
    public string Info { get; }

    public ConnectorStartedEventArgs(IConnector connector, string info)
    {
        Connector = connector;
        Info = info;
    }
}
