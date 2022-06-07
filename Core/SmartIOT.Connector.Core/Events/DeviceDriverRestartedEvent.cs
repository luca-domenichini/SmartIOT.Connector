namespace SmartIOT.Connector.Core.Events
{
	public class DeviceDriverRestartedEvent
	{
		public IDeviceDriver DeviceDriver { get; }
		public bool IsSuccess { get; }
		public string ErrorDescription { get; }

		public DeviceDriverRestartedEvent(IDeviceDriver deviceDriver, bool isSuccess, string errorDescription)
		{
			DeviceDriver = deviceDriver;
			IsSuccess = isSuccess;
			ErrorDescription = errorDescription;
		}
	}
}
