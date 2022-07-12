using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Extensions.ManagedClient;
using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Connector;
using SmartIOT.Connector.Core.Events;
using SmartIOT.Connector.Messages;
using SmartIOT.Connector.Messages.Serializers;

namespace SmartIOT.Connector.Mqtt.Client
{
	public class MqttClientConnector : AbstractPublisherConnector
	{
		private new MqttClientConnectorOptions Options => (MqttClientConnectorOptions)base.Options;
		private readonly ManagedMqttClientOptions _mqttOptions;
		private readonly IManagedMqttClient _mqttClient;
		private readonly ISingleMessageSerializer _messageSerializer;
		private bool _connected;

		public MqttClientConnector(MqttClientConnectorOptions options)
			: base(options)
		{
			_messageSerializer = options.MessageSerializer;

			MqttClientOptionsBuilder clientOptions = new MqttClientOptionsBuilder()
				.WithClientId(Options.ClientId)
				.WithTcpServer(Options.ServerAddress, Options.ServerPort);

			if (!string.IsNullOrWhiteSpace(Options.Username) && !string.IsNullOrWhiteSpace(Options.Password))
				clientOptions = clientOptions.WithCredentials(Options.Username, Options.Password);

			_mqttOptions = new ManagedMqttClientOptionsBuilder()
				.WithAutoReconnectDelay(Options.ReconnectDelay)
				.WithClientOptions(clientOptions.Build())
				.Build();

			_mqttClient = new MqttFactory().CreateManagedMqttClient();

			_mqttClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(OnConnected);
			_mqttClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(OnDisconnected);
			_mqttClient.ConnectingFailedHandler = new ConnectingFailedHandlerDelegate(OnConnectionFailed);
			_mqttClient.UseApplicationMessageReceivedHandler(e => OnApplicationMessageReceived(e));
		}

		public override void Start(ISmartIOTConnectorInterface connectorInterface)
		{
			base.Start(connectorInterface);

			_mqttClient.StartAsync(_mqttOptions);
		}

		public override void Stop()
		{
			base.Stop();

			_mqttClient.StopAsync().Wait();
		}

		private void OnConnectionFailed(ManagedProcessFailedEventArgs e)
		{
			ConnectorInterface!.OnConnectorConnectionFailed(new ConnectorConnectionFailedEventArgs(this, $"ClientId {Options.ClientId} failed connection to server {Options.ServerAddress}:{Options.ServerPort}: {e.Exception.Message}", e.Exception));
		}

		private void OnDisconnected(MqttClientDisconnectedEventArgs e)
		{
			_connected = false;
			ConnectorInterface!.OnConnectorDisconnected(new ConnectorDisconnectedEventArgs(this, $"ClientId {Options.ClientId} disconnected from server {Options.ServerAddress}:{Options.ServerPort}: {e.ConnectResult?.ReasonString ?? "NULL"}", e.Exception));
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

		private void OnConnected(MqttClientConnectedEventArgs e)
		{
			_mqttClient.SubscribeAsync(Options.TagWriteRequestCommandsTopicRoot, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce).Wait();

			ConnectorInterface!.RunInitializationAction(
				initAction: (deviceEvents, tagEvents) =>
				{
					foreach (var deviceEvent in deviceEvents)
					{
						PublishDeviceStatusEvent(deviceEvent, true);
					}
					foreach (var tagEvent in tagEvents)
					{
						PublishTagScheduleEvent(tagEvent, true);
					}
				});

			_connected = true; // once initialized the publisher is OK
			ConnectorInterface!.OnConnectorConnected(new ConnectorConnectedEventArgs(this, $"ClientId {Options.ClientId} connected to server {Options.ServerAddress}:{Options.ServerPort}"));
		}

		protected override void PublishException(Exception exception)
		{
			if (_connected)
			{
				_mqttClient.PublishAsync(b => b
					.WithTopic(Options.ExceptionsTopicPattern)
					.WithAtLeastOnceQoS()
					.WithPayload(_messageSerializer.SerializeMessage(EventExtensions.ToEventMessage(exception)))
				).Wait();
			}
		}

		protected override void PublishDeviceStatusEvent(DeviceStatusEvent e)
		{
			PublishDeviceStatusEvent(e, false);
		}
		private void PublishDeviceStatusEvent(DeviceStatusEvent e, bool isInitializationEvent)
		{
			if (_connected || isInitializationEvent)
			{
				_mqttClient.PublishAsync(b => b
					.WithTopic(Options.GetDeviceStatusEventsTopic(e.Device.DeviceId))
					.WithAtLeastOnceQoS()
					.WithPayload(_messageSerializer.SerializeMessage(EventExtensions.ToEventMessage(e)))
					.WithRetainFlag(true)
				).Wait();
			}
		}

		protected override void PublishTagScheduleEvent(TagScheduleEvent e)
		{
			PublishTagScheduleEvent(e, false);
		}
		private void PublishTagScheduleEvent(TagScheduleEvent e, bool isInitializationData)
		{
			if (_connected || isInitializationData)
			{
				// il client Mqtt deve trasmettere per forza ogni volta tutto il tag con il flag di retain
				// in modo che il server lo ripubblichi ai client connessi.
				var evt = e.Data != null ? TagScheduleEvent.BuildTagData(e.Device, e.Tag, e.IsErrorNumberChanged) : e;

				var message = evt.ToEventMessage(isInitializationData);

				_mqttClient.PublishAsync(b => b
					.WithTopic(Options.GetTagScheduleEventsTopic(e.Device.DeviceId, e.Tag.TagId))
					.WithAtLeastOnceQoS()
					.WithPayload(_messageSerializer.SerializeMessage(message))
					.WithRetainFlag(true)).Wait();
			}
		}
	}
}
