namespace SmartIOT.Connector.Core.Events
{
    public class ConnectorExceptionEventArgs : EventArgs
    {
        public IConnector Connector { get; }
        public Exception Exception { get; }

        public ConnectorExceptionEventArgs(IConnector connector, Exception exception)
        {
            Connector = connector;
            Exception = exception;
        }
    }
}
