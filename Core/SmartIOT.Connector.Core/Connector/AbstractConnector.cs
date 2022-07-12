using SmartIOT.Connector.Core.Events;

namespace SmartIOT.Connector.Core.Connector
{
	public abstract class AbstractConnector : IConnector
	{
		public string ConnectionString { get; }

		protected AbstractConnector(string connectionString)
		{
			ConnectionString = connectionString;
		}

		public abstract void Start(ISmartIOTConnectorInterface connectorInterface);
		public abstract void Stop();
		public abstract void OnTagReadEvent(object? sender, TagScheduleEventArgs args);
		public abstract void OnTagWriteEvent(object? sender, TagScheduleEventArgs args);
		public abstract void OnDeviceStatusEvent(object? sender, DeviceStatusEventArgs args);
		public abstract void OnException(object? sender, ExceptionEventArgs args);
	}
}
