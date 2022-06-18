using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Server;
using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Connector;
using SmartIOT.Connector.Core.Events;
using SmartIOT.Connector.Messages;
using SmartIOT.Connector.Messages.Serializers;

namespace SmartIOT.Connector.Mqtt.Server
{
	public class MqttServerConnector : AbstractPublisherConnector
	{
		private new MqttServerConnectorOptions Options => (MqttServerConnectorOptions)base.Options;
		private readonly IMqttServerOptions _mqttOptions;
		private readonly IMqttServer _mqttServer;
		private readonly ISingleMessageSerializer _messageSerializer;
		private bool _started;

		public MqttServerConnector(MqttServerConnectorOptions options)
			: base(options)
		{
			_messageSerializer = options.MessageSerializer;

			MqttServerOptionsBuilder serverOptions = new MqttServerOptionsBuilder()
				.WithClientId(Options.ServerId)
				.WithDefaultEndpointPort(Options.ServerPort);

			_mqttOptions = serverOptions.Build();

			_mqttServer = new MqttFactory().CreateMqttServer();

			_mqttServer.UseClientConnectedHandler(e => OnClientConnected(e));
			_mqttServer.UseClientDisconnectedHandler(e => OnClientDisconnected(e));
			_mqttServer.UseApplicationMessageReceivedHandler(e => OnApplicationMessageReceived(e));

			_mqttServer.ClientSubscribedTopicHandler = new MqttServerClientSubscribedTopicHandlerDelegate(OnClientSubscribedTopic);

			_mqttServer.StartedHandler = new MqttServerStartedHandlerDelegate(OnStarted);
			_mqttServer.StoppedHandler = new MqttServerStoppedHandlerDelegate(OnStopped);
		}

		private void OnClientSubscribedTopic(MqttServerClientSubscribedTopicEventArgs e)
		{
			if (Options.IsDeviceStatusEventsTopicRoot(e.TopicFilter.Topic))
			{
				InvokeInitializationDelegate(true, false);
			}
			if (Options.IsTagScheduleEventsTopicRoot(e.TopicFilter.Topic))
			{
				InvokeInitializationDelegate(false, true);
			}
		}

		public override void Start(ISmartIOTConnectorInterface connectorInterface)
		{
			base.Start(connectorInterface);

			_mqttServer.StartAsync(_mqttOptions).Wait();
		}

		private void OnStarted(EventArgs obj)
		{
			_started = true;
		}

		public override void Stop()
		{
			base.Stop();

			_mqttServer.StopAsync().Wait();
		}

		private void OnStopped(EventArgs obj)
		{
			_started = false;
		}

		private void OnApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e)
		{
			if (e.ApplicationMessage.Topic.StartsWith(Options.TagWriteRequestCommandsTopicRoot, StringComparison.InvariantCultureIgnoreCase))
			{
				var command = _messageSerializer.DeserializeMessage<TagWriteRequestCommand>(e.ApplicationMessage.Payload);
				if (command != null)
					ConnectorInterface!.RequestTagWrite(command.DeviceId, command.TagId, command.StartOffset, command.Data);
			}
		}

		private void OnClientConnected(MqttServerClientConnectedEventArgs e)
		{
			ConnectorInterface!.OnConnectorConnected(new ConnectorConnectedEventArgs(this, $"ClientId {e.ClientId} connected to port {Options.ServerPort}"));
		}

		private void OnClientDisconnected(MqttServerClientDisconnectedEventArgs e)
		{
			ConnectorInterface!.OnConnectorDisconnected(new ConnectorDisconnectedEventArgs(this, $"ClientId {e.ClientId} disconnected: {e.DisconnectType}"));
		}

		protected override void PublishException(Exception exception)
		{
			if (_started)
			{
				try
				{
					_mqttServer.PublishAsync(b => b
						.WithTopic(Options.ExceptionsTopicPattern)
						.WithAtLeastOnceQoS()
						.WithPayload(_messageSerializer.SerializeMessage(EventExtensions.ToEventMessage(exception)))
					).Wait();
				}
				catch (Exception ex)
				{
					OnException(ex);
				}
			}
		}

		protected override void PublishDeviceStatusEvent(DeviceStatusEvent e)
		{
			if (_started)
			{
				try
				{
					_mqttServer.PublishAsync(b => b
						.WithTopic(Options.GetDeviceStatusEventsTopic(e.Device.DeviceId))
						.WithAtLeastOnceQoS()
						.WithPayload(_messageSerializer.SerializeMessage(EventExtensions.ToEventMessage(e)))
					).Wait();
				}
				catch (Exception ex)
				{
					OnException(ex);
				}
			}
		}

		protected override void PublishTagScheduleEvent(TagScheduleEvent e)
		{
			PublishTagScheduleEvent(e, false);
		}
		private void PublishTagScheduleEvent(TagScheduleEvent e, bool isInitializationData)
		{
			if (_started)
			{
				var evt = !Options.IsPublishPartialReads && e.Data != null ? TagScheduleEvent.BuildTagData(e.Device, e.Tag, e.IsErrorNumberChanged) : e;

				var message = evt.ToEventMessage(isInitializationData);

				try
				{
					_mqttServer.PublishAsync(b => b
						.WithTopic(Options.GetTagScheduleEventsTopic(e.Device.DeviceId, e.Tag.TagId))
						.WithAtLeastOnceQoS()
						.WithPayload(_messageSerializer.SerializeMessage(message))
					).Wait();
				}
				catch (Exception ex)
				{
					OnException(ex);
				}
			}
		}

		private void InvokeInitializationDelegate(bool publishDeviceStatusEvents, bool publishTagScheduleEvents)
		{
			ConnectorInterface!.RunInitializationAction(
				initAction: (deviceEvents, tagEvents) =>
				{
					if (publishDeviceStatusEvents)
					{
						foreach (var deviceEvent in deviceEvents)
						{
							PublishDeviceStatusEvent(deviceEvent);
						}
					}
					if (publishTagScheduleEvents)
					{
						foreach (var tagEvent in tagEvents)
						{
							PublishTagScheduleEvent(tagEvent, true);
						}
					}
				});
		}

		private void OnException(Exception ex)
		{
			try
			{
				ConnectorInterface!.OnConnectorException(new ConnectorExceptionEventArgs(this, ex));
			}
			catch
			{
				// ignoring this
			}
		}

	}
}
