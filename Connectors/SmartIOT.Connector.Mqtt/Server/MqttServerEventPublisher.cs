﻿using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Server;
using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Connector;
using SmartIOT.Connector.Core.Events;
using SmartIOT.Connector.Messages;
using SmartIOT.Connector.Messages.Serializers;

namespace SmartIOT.Connector.Mqtt.Server
{
	public class MqttServerEventPublisher : IConnectorEventPublisher
	{
		private readonly MqttServerEventPublisherOptions _options;
		private readonly IMqttServerOptions _mqttOptions;
		private readonly IMqttServer _mqttServer;
		private readonly ISingleMessageSerializer _messageSerializer;
		private bool _started;
		private IConnector? _connector;
		private ISmartIOTConnectorInterface? _connectorInterface;

		public MqttServerEventPublisher(ISingleMessageSerializer messageSerializer, MqttServerEventPublisherOptions options)
		{
			_messageSerializer = messageSerializer;
			_options = options;

			MqttServerOptionsBuilder serverOptions = new MqttServerOptionsBuilder()
				.WithClientId(_options.ServerId)
				.WithDefaultEndpointPort(_options.ServerPort);

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
			if (_options.IsDeviceStatusEventsTopicRoot(e.TopicFilter.Topic))
			{
				InvokeInitializationDelegate(true, false);
			}
			if (_options.IsTagScheduleEventsTopicRoot(e.TopicFilter.Topic))
			{
				InvokeInitializationDelegate(false, true);
			}
		}

		public void Start(IConnector connector, ISmartIOTConnectorInterface connectorInterface)
		{
			_connector = connector;
			_connectorInterface = connectorInterface;
			_mqttServer.StartAsync(_mqttOptions).Wait();
		}

		private void OnStarted(EventArgs obj)
		{
			_started = true;
		}

		public void Stop()
		{
			_mqttServer.StopAsync().Wait();
		}

		private void OnStopped(EventArgs obj)
		{
			_started = false;
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

		private void OnClientConnected(MqttServerClientConnectedEventArgs e)
		{
			_connectorInterface!.OnConnectorConnected(new ConnectorConnectedEventArgs(_connector!, $"ClientId {e.ClientId} connected to port {_options.ServerPort}"));
		}

		private void OnClientDisconnected(MqttServerClientDisconnectedEventArgs e)
		{
			_connectorInterface!.OnConnectorDisconnected(new ConnectorDisconnectedEventArgs(_connector!, $"ClientId {e.ClientId} disconnected: {e.DisconnectType}"));
		}

		public void PublishException(Exception exception)
		{
			if (_started)
			{
				try
				{
					_mqttServer.PublishAsync(b => b
						.WithTopic(_options.ExceptionsTopicPattern)
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

		public void PublishDeviceStatusEvent(DeviceStatusEvent e)
		{
			if (_started)
			{
				try
				{
					_mqttServer.PublishAsync(b => b
						.WithTopic(_options.GetDeviceStatusEventsTopic(e.Device.DeviceId))
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

		public void PublishTagScheduleEvent(TagScheduleEvent e)
		{
			PublishTagScheduleEvent(e, false);
		}
		public void PublishTagScheduleEvent(TagScheduleEvent e, bool isInitializationData)
		{
			if (_started)
			{
				var evt = !_options.IsPublishPartialReads && e.Data != null ? TagScheduleEvent.BuildTagData(e.Device, e.Tag, e.IsErrorNumberChanged) : e;

				var message = evt.ToEventMessage(isInitializationData);

				try
				{
					_mqttServer.PublishAsync(b => b
						.WithTopic(_options.GetTagScheduleEventsTopic(e.Device.DeviceId, e.Tag.TagId))
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
			_connectorInterface!.RunInitializationAction(
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
				_connectorInterface!.OnConnectorException(new ConnectorExceptionEventArgs(_connector!, ex));
			}
			catch
			{
				// ignoring this
			}
		}

	}
}
