using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Events;
using SmartIOT.Connector.Core.Queue;

namespace SmartIOT.Connector.Mqtt
{
	public class MqttSchedulerConnector : AbstractBufferedConnector
	{
		public MqttSchedulerConnectorOptions Options { get; }
		public IMqttEventPublisher MqttEventPublisher { get; }

		public MqttSchedulerConnector(MqttSchedulerConnectorOptions options, IMqttEventPublisher mqttEventPublisher)
		{
			Options = options;
			MqttEventPublisher = mqttEventPublisher;
		}

		public override void Start(ConnectorInterface connectorInterface)
		{
			base.Start(connectorInterface);

			MqttEventPublisher.Start(this, connectorInterface);
		}
		public override void Stop()
		{
			base.Stop();

			MqttEventPublisher.Stop();
		}

		protected override void HandleOnException(object? sender, ExceptionEventArgs args)
		{
			MqttEventPublisher.PublishException(args.Exception);
		}

		protected override void HandleOnDeviceStatusEvent(object? sender, DeviceStatusEventArgs args)
		{
			MqttEventPublisher.PublishDeviceStatusEvent(args.DeviceStatusEvent);
		}

		protected override void HandleOnTagReadEvent(object? sender, TagScheduleEventArgs args)
		{
			MqttEventPublisher.PublishTagScheduleEvent(args.TagScheduleEvent);
		}
		protected override void HandleOnTagWriteEvent(object? sender, TagScheduleEventArgs args)
		{
			if (Options.IsPublishWriteEvents)
				MqttEventPublisher.PublishTagScheduleEvent(args.TagScheduleEvent);
		}
	}
}
