namespace SmartIOT.Connector.Core.Events;

public class ConnectorConnectedEventArgs : EventArgs
{
    public IConnector Connector { get; }
    public string Info { get; }

    public ConnectorConnectedEventArgs(IConnector connector, string info)
    {
        Connector = connector;
        Info = info;
    }
}
