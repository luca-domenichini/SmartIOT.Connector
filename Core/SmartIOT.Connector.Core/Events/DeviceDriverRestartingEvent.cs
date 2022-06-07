namespace SmartIOT.Connector.Core.Events
{
	public class DeviceDriverRestartingEvent
	{
		public IDeviceDriver DeviceDriver { get; }

		public DeviceDriverRestartingEvent(IDeviceDriver deviceDriver)
		{
			DeviceDriver = deviceDriver;
		}
	}
}
