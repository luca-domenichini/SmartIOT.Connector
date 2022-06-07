using SmartIOT.Connector.Core.Events;

namespace SmartIOT.Connector.Core.Queue
{
	public class CompositeConnectorEvent
	{
		public (object? sender, TagScheduleEventArgs args)? TagReadScheduleEvent { get; init; }
		public (object? sender, TagScheduleEventArgs args)? TagWriteScheduleEvent { get; init; }
		public (object? sender, DeviceStatusEventArgs args)? DeviceStatusEvent { get; init; }
		public (object? sender, ExceptionEventArgs args)? ExceptionEvent { get; init; }

		public static CompositeConnectorEvent TagRead((object? sender, TagScheduleEventArgs args) e)
		{
			return TagRead(e, e.args.TagScheduleEvent.IsErrorNumberChanged);
		}
		public static CompositeConnectorEvent TagRead((object? sender, TagScheduleEventArgs args) e, bool isErrorNumberChanged)
		{
			var ee = new CompositeConnectorEvent()
			{
				TagReadScheduleEvent = e
			};
			ee.TagReadScheduleEvent.Value.args.TagScheduleEvent.IsErrorNumberChanged = isErrorNumberChanged;

			return ee;
		}

		public static CompositeConnectorEvent TagWrite((object? sender, TagScheduleEventArgs args) e)
		{
			return new CompositeConnectorEvent()
			{
				TagWriteScheduleEvent = e
			};
		}

		public static CompositeConnectorEvent DeviceStatus((object? sender, DeviceStatusEventArgs args) e)
		{
			return new CompositeConnectorEvent()
			{
				DeviceStatusEvent = e
			};
		}

		public static CompositeConnectorEvent Exception((object? sender, ExceptionEventArgs args) e)
		{
			return new CompositeConnectorEvent()
			{
				ExceptionEvent = e
			};
		}
	}
}
