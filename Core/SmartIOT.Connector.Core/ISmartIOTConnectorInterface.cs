using SmartIOT.Connector.Core.Events;

#pragma warning disable S101 // Types should be named in PascalCase: we named this SmartIOT, that's it.

namespace SmartIOT.Connector.Core
{
    /// <summary>
    /// This interface represents the conjuction point between SmartIOT Connector and Connectors used to communicate with external systems.
    /// </summary>
    public interface ISmartIOTConnectorInterface
    {
        /// <summary>
        /// This method request SmartIOT Connector to execute the initAction by passing the list of DeviceStatusEvents and TagScheduleEvents
        /// to initialize the underlying Connector.
        /// This methd is typically used when the external system connects to the connector and it needs the most recent
        /// data to initialize.
        /// </summary>
        /// <param name="initAction">Action executed for each scheduler registered in the main SmartIOT Connector</param>
        public Task RunInitializationActionAsync(Func<IList<DeviceStatusEvent>, IList<TagScheduleEvent>, Task> initAction);

        /// <summary>
        /// The method is to be called when the connector wants to request data write to the main SmartIOT Connector.
        /// </summary>
        public void RequestTagWrite(string deviceId, string tagId, int startOffset, byte[] data);

        /// <summary>
        /// Method to be called when a Connector starts
        /// </summary>
        public void OnConnectorStarted(ConnectorStartedEventArgs args);

        /// <summary>
        /// Method to be called when a Connector stops
        /// </summary>
        public void OnConnectorStopped(ConnectorStoppedEventArgs args);

        /// <summary>
        /// Method to be called when a Connector connects to an external system
        /// </summary>
        public void OnConnectorConnected(ConnectorConnectedEventArgs args);

        /// <summary>
        /// Method to be called when a Connector fails to connect to an external sysyem
        /// </summary>
        public void OnConnectorConnectionFailed(ConnectorConnectionFailedEventArgs args);

        /// <summary>
        /// Method to be called when a Connector disconnects from an external sysyem
        /// </summary>
        public void OnConnectorDisconnected(ConnectorDisconnectedEventArgs args);

        /// <summary>
        /// Method to be called when an unexpected Exception occurs in a Connector
        /// </summary>
        public void OnConnectorException(ConnectorExceptionEventArgs args);
    }
}
