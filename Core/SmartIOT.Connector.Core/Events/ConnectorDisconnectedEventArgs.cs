namespace SmartIOT.Connector.Core.Events
{
    public class ConnectorDisconnectedEventArgs : EventArgs
    {
        public IConnector Connector { get; }
        public string Info { get; }
        public Exception? Exception { get; }

        public ConnectorDisconnectedEventArgs(IConnector connector, string info)
            : this(connector, info, null)
        {
        }

        public ConnectorDisconnectedEventArgs(IConnector connector, string info, Exception? exception)
        {
            Connector = connector;
            Info = info;
            Exception = exception;
        }
    }
}
