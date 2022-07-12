using SmartIOT.Connector.Core.Events;

namespace SmartIOT.Connector.Core.Connector
{
	public abstract class AbstractPublisherConnector : AbstractBufferedAggregatingConnector
	{
		public ConnectorOptions Options { get; }

		public AbstractPublisherConnector(ConnectorOptions options)
			: base(options.ConnectionString)
		{
			Options = options;
		}

		public override void Start(ISmartIOTConnectorInterface connectorInterface)
		{
			base.Start(connectorInterface);
		}
		public override void Stop()
		{
			base.Stop();
		}

		protected abstract void PublishTagScheduleEvent(TagScheduleEvent e);
		protected abstract void PublishDeviceStatusEvent(DeviceStatusEvent e);
		protected abstract void PublishException(Exception exception);


		protected override void HandleOnException(object? sender, ExceptionEventArgs args)
		{
			PublishException(args.Exception);
		}

		protected override void HandleOnDeviceStatusEvent(object? sender, DeviceStatusEventArgs args)
		{
			PublishDeviceStatusEvent(args.DeviceStatusEvent);
		}

		protected override void HandleOnTagReadEvent(object? sender, TagScheduleEventArgs args)
		{
			PublishTagScheduleEvent(args.TagScheduleEvent);
		}
		protected override void HandleOnTagWriteEvent(object? sender, TagScheduleEventArgs args)
		{
			if (Options.IsPublishWriteEvents)
				PublishTagScheduleEvent(args.TagScheduleEvent);
		}
	}
}
