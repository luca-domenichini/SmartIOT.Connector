using SmartIOT.Connector.Messages;

namespace SmartIOT.Connector.Core.Events
{
	public static class EventExtensions
	{
		public static ExceptionEvent ToEventMessage(this Exception exception)
		{
			return new ExceptionEvent(exception.Message, exception.ToString());
		}

		public static DeviceEvent ToEventMessage(this DeviceStatusEvent deviceStatusEvent)
		{
			return new DeviceEvent(deviceStatusEvent.Device.DeviceId, deviceStatusEvent.ConvertToDeviceStatus(), deviceStatusEvent.ErrorCode, deviceStatusEvent.Description);
		}

		private static DeviceStatus ConvertToDeviceStatus(this DeviceStatusEvent deviceStatusEvent)
		{
			switch (deviceStatusEvent.DeviceStatus)
			{
				case Model.DeviceStatus.UNINITIALIZED:
					return DeviceStatus.UNINITIALIZED;
				case Model.DeviceStatus.OK:
					return DeviceStatus.OK;
				case Model.DeviceStatus.ERROR:
					return DeviceStatus.ERROR;
				case Model.DeviceStatus.DISABLED:
					break;
			}
			return DeviceStatus.UNINITIALIZED;
		}

		public static TagEvent ToEventMessage(this TagScheduleEvent e, bool isInitializationData = false)
		{
			if (e.Data == null)
				return TagEvent.CreateTagStatusEvent(e.Device.DeviceId, e.Tag.TagId, e.ErrorNumber, e.Description ?? string.Empty);

			return TagEvent.CreateTagDataEvent(e.Device.DeviceId, e.Tag.TagId, e.StartOffset, e.Data, isInitializationData);
		}
	}
}
