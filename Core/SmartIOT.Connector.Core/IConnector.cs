using SmartIOT.Connector.Core.Events;

namespace SmartIOT.Connector.Core
{
	public interface IConnector
	{
		string ConnectionString { get; }

		/// <summary>
		/// Invocato per indicare all'handler la partenza delle attività
		/// </summary>
		public void Start(ISmartIOTConnectorInterface connectorInterface);
		/// <summary>
		/// Invocato per indicare all'handler l'arresto delle attività
		/// </summary>
		public void Stop();

		public void OnTagReadEvent(object? sender, TagScheduleEventArgs args);
		public void OnTagWriteEvent(object? sender, TagScheduleEventArgs args);
		public void OnDeviceStatusEvent(object? sender, DeviceStatusEventArgs args);
		public void OnException(object? sender, ExceptionEventArgs args);
	}
}
