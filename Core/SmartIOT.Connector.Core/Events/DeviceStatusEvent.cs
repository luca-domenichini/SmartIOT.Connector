using SmartIOT.Connector.Core.Model;

namespace SmartIOT.Connector.Core.Events
{
	public class DeviceStatusEvent
	{
		public Device Device { get; }
		public Model.DeviceStatus DeviceStatus { get; }
		public int ErrorCode { get; }
		public string Description { get; }

		public DeviceStatusEvent(Device device, Model.DeviceStatus deviceStatus, int errorNumber, string description)
		{
			Device = device;
			DeviceStatus = deviceStatus;
			ErrorCode = errorNumber;
			Description = description;
		}
	}
}
