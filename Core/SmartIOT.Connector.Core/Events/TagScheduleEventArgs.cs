namespace SmartIOT.Connector.Core.Events
{
	public class TagScheduleEventArgs : EventArgs
	{
		public IDeviceDriver DeviceDriver { get; }
		public TagScheduleEvent TagScheduleEvent { get; }

		public TagScheduleEventArgs(IDeviceDriver deviceDriver, TagScheduleEvent tagScheduleEvent)
		{
			DeviceDriver = deviceDriver;
			TagScheduleEvent = tagScheduleEvent;
		}
	}
}
