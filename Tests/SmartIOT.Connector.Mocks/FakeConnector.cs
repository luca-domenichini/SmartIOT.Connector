using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Connector;
using SmartIOT.Connector.Core.Events;

namespace SmartIOT.Connector.Mocks
{
	public class FakeConnector : AbstractConnector
	{
		private ISmartIOTConnectorInterface? _connectorInterface;

		public IList<TagScheduleEvent> TagReadEvents { get; } = new List<TagScheduleEvent>();
		public IList<TagScheduleEvent> TagWriteEvents { get; } = new List<TagScheduleEvent>();
		public IList<DeviceStatusEvent> DeviceStatusEvents { get; } = new List<DeviceStatusEvent>();
		public IList<ExceptionEventArgs> ExceptionEvents { get; } = new List<ExceptionEventArgs>();

		public FakeConnector() : base("fake://")
		{
		}

		public override void OnTagReadEvent(object? sender, TagScheduleEventArgs e)
		{
			TagReadEvents.Add(e.TagScheduleEvent);
		}

		public override void OnTagWriteEvent(object? sender, TagScheduleEventArgs e)
		{
			TagWriteEvents.Add(e.TagScheduleEvent);
		}

		public override void OnDeviceStatusEvent(object? sender, DeviceStatusEventArgs e)
		{
			DeviceStatusEvents.Add(e.DeviceStatusEvent);
		}

		public override void OnException(object? sender, ExceptionEventArgs args)
		{
			ExceptionEvents.Add(args);
		}

		public void ClearEvents()
		{
			TagReadEvents.Clear();
			TagWriteEvents.Clear();
			DeviceStatusEvents.Clear();
			ExceptionEvents.Clear();
		}

		public override void Start(ISmartIOTConnectorInterface connectorInterface)
		{
			_connectorInterface = connectorInterface;
		}
		public override void Stop()
		{

		}

		public void RequestTagWrite(string deviceId, string tagId, int startOffset, byte[] data)
		{
			_connectorInterface!.RequestTagWrite(deviceId, tagId, startOffset, data);
		}
	}
}
