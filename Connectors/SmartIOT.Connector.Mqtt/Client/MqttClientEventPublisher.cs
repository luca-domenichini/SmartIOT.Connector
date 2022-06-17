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
	public class MqttClientEventPublisher : IConnectorEventPublisher
	{
		private readonly MqttClientEventPublisherOptions _options;
		private readonly ManagedMqttClientOptions _mqttOptions;
		private readonly IManagedMqttClient _mqttClient;
		private readonly ISingleMessageSerializer _messageSerializer;
		private bool _connected;
		private IConnector? _connector;
		private ISmartIOTConnectorInterface? _connectorInterface;

		public MqttClientEventPublisher(ISingleMessageSerializer messageSerializer, MqttClientEventPublisherOptions options)
		{
			_messageSerializer = messageSerializer;
			_options = options;

			MqttClientOptionsBuilder clientOptions = new MqttClientOptionsBuilder()
				.WithClientId(_options.ClientId)
				.WithTcpServer(_options.ServerAddress, _options.ServerPort);

			if (!string.IsNullOrWhiteSpace(_options.Username) && !string.IsNullOrWhiteSpace(_options.Password))
				clientOptions = clientOptions.WithCredentials(_options.Username, _options.Password);

			_mqttOptions = new ManagedMqttClientOptionsBuilder()
				.WithAutoReconnectDelay(_options.ReconnectDelay)
				.WithClientOptions(clientOptions.Build())
				.Build();

			_mqttClient = new MqttFactory().CreateManagedMqttClient();

			_mqttClient.ConnectedHandler = new MqttClientConnectedHandlerDelegate(OnConnected);
			_mqttClient.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(OnDisconnected);
			_mqttClient.ConnectingFailedHandler = new ConnectingFailedHandlerDelegate(OnConnectionFailed);
			_mqttClient.UseApplicationMessageReceivedHandler(e => OnApplicationMessageReceived(e));
		}

		public void Start(IConnector connector, ISmartIOTConnectorInterface connectorInterface)
		{
			_connector = connector;
			_connectorInterface = connectorInterface;
			_mqttClient.StartAsync(_mqttOptions);
		}

		public void Stop()
		{
			_mqttClient.StopAsync().Wait();
		}

		private void OnConnectionFailed(ManagedProcessFailedEventArgs e)
		{
			_connectorInterface!.OnConnectorConnectionFailed(new ConnectorConnectionFailedEventArgs(_connector!, $"ClientId {_options.ClientId} failed connection to server {_options.ServerAddress}:{_options.ServerPort}: {e.Exception.Message}", e.Exception));
		}

		private void OnDisconnected(MqttClientDisconnectedEventArgs e)
		{
			_connected = false;
			_connectorInterface!.OnConnectorDisconnected(new ConnectorDisconnectedEventArgs(_connector!, $"ClientId {_options.ClientId} disconnected from server {_options.ServerAddress}:{_options.ServerPort}: {e.ConnectResult.ReasonString}", e.Exception));
		}

		private void OnApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e)
		{
			if (e.ApplicationMessage.Topic.StartsWith(_options.TagWriteRequestCommandsTopicRoot, StringComparison.InvariantCultureIgnoreCase))
			{
				var command = _messageSerializer.DeserializeMessage<TagWriteRequestCommand>(e.ApplicationMessage.Payload);
				if (command != null)
					_connectorInterface!.RequestTagWrite(command.DeviceId, command.TagId, command.StartOffset, command.Data);
			}
		}

		private void OnConnected(MqttClientConnectedEventArgs e)
		{
			_mqttClient.SubscribeAsync(_options.TagWriteRequestCommandsTopicRoot, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce).Wait();

			_connectorInterface!.RunInitializationAction(
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
			_connectorInterface!.OnConnectorConnected(new ConnectorConnectedEventArgs(_connector!, $"ClientId {_options.ClientId} connected to server {_options.ServerAddress}:{_options.ServerPort}"));
		}

		public void PublishException(Exception exception)
		{
			if (_connected)
			{
				_mqttClient.PublishAsync(b => b
					.WithTopic(_options.ExceptionsTopicPattern)
					.WithAtLeastOnceQoS()
					.WithPayload(_messageSerializer.SerializeMessage(EventExtensions.ToEventMessage(exception)))
				).Wait();
			}
		}

		public void PublishDeviceStatusEvent(DeviceStatusEvent e)
		{
			PublishDeviceStatusEvent(e, false);
		}
		public void PublishDeviceStatusEvent(DeviceStatusEvent e, bool isInitializationEvent)
		{
			if (_connected || isInitializationEvent)
			{
				_mqttClient.PublishAsync(b => b
					.WithTopic(_options.GetDeviceStatusEventsTopic(e.Device.DeviceId))
					.WithAtLeastOnceQoS()
					.WithPayload(_messageSerializer.SerializeMessage(EventExtensions.ToEventMessage(e)))
					.WithRetainFlag(true)
				).Wait();
			}
		}

		public void PublishTagScheduleEvent(TagScheduleEvent e)
		{
			PublishTagScheduleEvent(e, false);
		}
		public void PublishTagScheduleEvent(TagScheduleEvent e, bool isInitializationData)
		{
			if (_connected || isInitializationData)
			{
				// il client Mqtt deve trasmettere per forza ogni volta tutto il tag con il flag di retain
				// in modo che il server lo ripubblichi ai client connessi.
				var evt = e.Data != null ? TagScheduleEvent.BuildTagData(e.Device, e.Tag, e.IsErrorNumberChanged) : e;

				var message = evt.ToEventMessage(isInitializationData);

				_mqttClient.PublishAsync(b => b
					.WithTopic(_options.GetTagScheduleEventsTopic(e.Device.DeviceId, e.Tag.TagId))
					.WithAtLeastOnceQoS()
					.WithPayload(_messageSerializer.SerializeMessage(message))
					.WithRetainFlag(true)).Wait();
			}
		}
	}
}
