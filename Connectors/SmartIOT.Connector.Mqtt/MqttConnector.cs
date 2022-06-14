using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Connector;
using SmartIOT.Connector.Core.Events;

namespace SmartIOT.Connector.Mqtt
{
	public class MqttConnector : AbstractBufferedAggregatingConnector
	{
		public MqttConnectorOptions Options { get; }
		public IMqttEventPublisher EventPublisher { get; }

		public MqttConnector(MqttConnectorOptions options, IMqttEventPublisher eventPublisher)
		{
			Options = options;
			EventPublisher = eventPublisher;
		}

		public override void Start(ConnectorInterface connectorInterface)
		{
			base.Start(connectorInterface);

			EventPublisher.Start(this, connectorInterface);
		}
		public override void Stop()
		{
			base.Stop();

			EventPublisher.Stop();
		}

		protected override void HandleOnException(object? sender, ExceptionEventArgs args)
		{
			EventPublisher.PublishException(args.Exception);
		}

		protected override void HandleOnDeviceStatusEvent(object? sender, DeviceStatusEventArgs args)
		{
			EventPublisher.PublishDeviceStatusEvent(args.DeviceStatusEvent);
		}

		protected override void HandleOnTagReadEvent(object? sender, TagScheduleEventArgs args)
		{
			EventPublisher.PublishTagScheduleEvent(args.TagScheduleEvent);
		}
		protected override void HandleOnTagWriteEvent(object? sender, TagScheduleEventArgs args)
		{
			if (Options.IsPublishWriteEvents)
				EventPublisher.PublishTagScheduleEvent(args.TagScheduleEvent);
		}
	}
}
