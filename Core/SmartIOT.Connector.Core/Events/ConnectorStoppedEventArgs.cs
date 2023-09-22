namespace SmartIOT.Connector.Core.Events
{
    public class ConnectorStoppedEventArgs : EventArgs
    {
        public IConnector Connector { get; }
        public string Info { get; }

        public ConnectorStoppedEventArgs(IConnector connector, string info)
        {
            Connector = connector;
            Info = info;
        }
    }
}
