namespace SmartIOT.Connector.Core.Events
{
	public class DeviceDriverRestartedEventArgs : EventArgs
	{
		public DeviceDriverRestartedEvent Event { get; }

		public DeviceDriverRestartedEventArgs(DeviceDriverRestartedEvent @event)
		{
			Event = @event;
		}
	}
}
