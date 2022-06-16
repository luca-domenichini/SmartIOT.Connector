namespace SmartIOT.Connector.Core.Events
{
	public class DeviceDriverRestartedEventArgs : EventArgs
	{
		public IDeviceDriver DeviceDriver { get; }
		public bool IsSuccess { get; }
		public string ErrorDescription { get; }

		public DeviceDriverRestartedEventArgs(IDeviceDriver deviceDriver, bool isSuccess, string errorDescription)
		{
			DeviceDriver = deviceDriver;
			IsSuccess = isSuccess;
			ErrorDescription = errorDescription;
		}
	}
}
