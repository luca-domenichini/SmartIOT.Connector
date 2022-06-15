﻿using MQTTnet;
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
		private ConnectorInterface? _connectorInterface;

		manca l'evento onException?
		public event EventHandler<ManagedProcessFailedEventArgs>? ConnectionFailed;
		public event EventHandler<MqttClientDisconnectedEventArgs>? Disconnected;
		public event EventHandler<MqttClientConnectedEventArgs>? Connected;

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

		public void Start(IConnector connector, ConnectorInterface connectorInterface)
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
			ConnectionFailed?.Invoke(this, e);
		}

		private void OnDisconnected(MqttClientDisconnectedEventArgs e)
		{
			_connected = false;
			Disconnected?.Invoke(this, e);

			_connectorInterface?.DisconnectedDelegate.Invoke(_connector!, $"ClientId {_options.ClientId} disconnected from server {_options.ServerAddress}:{_options.ServerPort}");
		}

		private void OnApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e)
		{
			if (e.ApplicationMessage.Topic.StartsWith(_options.TagWriteRequestCommandsTopicRoot, StringComparison.InvariantCultureIgnoreCase))
			{
				var command = _messageSerializer.DeserializeMessage<TagWriteRequestCommand>(e.ApplicationMessage.Payload);
				if (command != null)
					_connectorInterface?.RequestTagWriteDelegate.Invoke(command.DeviceId, command.TagId, command.StartOffset, command.Data);
			}
		}

		private void OnConnected(MqttClientConnectedEventArgs e)
		{
			_mqttClient.SubscribeAsync(_options.TagWriteRequestCommandsTopicRoot, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce).Wait();

			_connectorInterface?.InitializationActionDelegate.Invoke(
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
				},
				afterInitAction: () =>
				{
					_connected = true; // una volta che ho inviato lo stato attuale, posso settare il publisher come OK
				});

			Connected?.Invoke(this, e);

			_connectorInterface?.ConnectedDelegate.Invoke(_connector!, $"ClientId {_options.ClientId} connected to server {_options.ServerAddress}:{_options.ServerPort}");
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
