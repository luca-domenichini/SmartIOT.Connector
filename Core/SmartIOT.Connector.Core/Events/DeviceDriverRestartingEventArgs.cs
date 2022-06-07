namespace SmartIOT.Connector.Core.Events
{
	public class DeviceDriverRestartingEventArgs : EventArgs
	{
		public DeviceDriverRestartingEvent Event { get; }

		public DeviceDriverRestartingEventArgs(DeviceDriverRestartingEvent @event)
		{
			Event = @event;
		}
	}
}
