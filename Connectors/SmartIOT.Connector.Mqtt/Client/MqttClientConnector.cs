using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Extensions.ManagedClient;
using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Connector;
using SmartIOT.Connector.Core.Events;
using SmartIOT.Connector.Messages;
using SmartIOT.Connector.Messages.Serializers;

namespace SmartIOT.Connector.Mqtt.Client;

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

        _mqttClient.ConnectedAsync += OnConnected;
        _mqttClient.DisconnectedAsync += OnDisconnected;
        _mqttClient.ConnectingFailedAsync += OnConnectionFailed;
        _mqttClient.ApplicationMessageReceivedAsync += OnApplicationMessageReceived;
    }

    public override async Task StartAsync(ISmartIOTConnectorInterface connectorInterface)
    {
        await base.StartAsync(connectorInterface);

        await _mqttClient.StartAsync(_mqttOptions);
    }

    public override async Task StopAsync()
    {
        await base.StopAsync();

        await _mqttClient.StopAsync();
    }

    private Task OnConnectionFailed(ConnectingFailedEventArgs e)
    {
        ConnectorInterface!.OnConnectorConnectionFailed(new ConnectorConnectionFailedEventArgs(this, $"ClientId {Options.ClientId} failed connection to server {Options.ServerAddress}:{Options.ServerPort}: {e.Exception.Message}", e.Exception));
        return Task.CompletedTask;
    }

    private Task OnDisconnected(MqttClientDisconnectedEventArgs e)
    {
        _connected = false;
        ConnectorInterface!.OnConnectorDisconnected(new ConnectorDisconnectedEventArgs(this, $"ClientId {Options.ClientId} disconnected from server {Options.ServerAddress}:{Options.ServerPort}: {e.ConnectResult?.ReasonString ?? "NULL"}", e.Exception));
        return Task.CompletedTask;
    }

    private Task OnApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        if (e.ApplicationMessage.Topic.StartsWith(Options.TagWriteRequestCommandsTopicRoot, StringComparison.InvariantCultureIgnoreCase))
        {
            var command = _messageSerializer.DeserializeMessage<TagWriteRequestCommand>(e.ApplicationMessage.PayloadSegment.Array!);
            if (command != null)
                ConnectorInterface!.RequestTagWrite(command.DeviceId, command.TagId, command.StartOffset, command.Data);
        }
        return Task.CompletedTask;
    }

    private async Task OnConnected(MqttClientConnectedEventArgs e)
    {
        await _mqttClient.SubscribeAsync(Options.TagWriteRequestCommandsTopicRoot, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);

        await ConnectorInterface!.RunInitializationActionAsync(
            initAction: async (deviceEvents, tagEvents) =>
            {
                foreach (var deviceEvent in deviceEvents)
                {
                    await PublishDeviceStatusEvent(deviceEvent, true);
                }
                foreach (var tagEvent in tagEvents)
                {
                    await PublishTagScheduleEvent(tagEvent, true);
                }
            });

        _connected = true; // once initialized the publisher is OK
        ConnectorInterface!.OnConnectorConnected(new ConnectorConnectedEventArgs(this, $"ClientId {Options.ClientId} connected to server {Options.ServerAddress}:{Options.ServerPort}"));
    }

    protected override async Task PublishExceptionAsync(Exception exception)
    {
        if (_connected)
        {
            await _mqttClient.EnqueueAsync(new MqttApplicationMessageBuilder()
                .WithTopic(Options.ExceptionsTopicPattern)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithPayload(_messageSerializer.SerializeMessage(EventExtensions.ToEventMessage(exception)))
                .Build()
            );
        }
    }

    protected override async Task PublishDeviceStatusEventAsync(DeviceStatusEvent e)
    {
        await PublishDeviceStatusEvent(e, false);
    }

    private async Task PublishDeviceStatusEvent(DeviceStatusEvent e, bool isInitializationEvent)
    {
        if (_connected || isInitializationEvent)
        {
            await _mqttClient.EnqueueAsync(new MqttApplicationMessageBuilder()
                .WithTopic(Options.GetDeviceStatusEventsTopic(e.Device.DeviceId))
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithPayload(_messageSerializer.SerializeMessage(EventExtensions.ToEventMessage(e)))
                .WithRetainFlag(true)
                .Build()
            );
        }
    }

    protected override async Task PublishTagScheduleEventAsync(TagScheduleEvent e)
    {
        await PublishTagScheduleEvent(e, false);
    }

    private async Task PublishTagScheduleEvent(TagScheduleEvent e, bool isInitializationData)
    {
        if (_connected || isInitializationData)
        {
            // il client Mqtt deve trasmettere per forza ogni volta tutto il tag con il flag di retain
            // in modo che il server lo ripubblichi ai client connessi.
            var evt = e.Data != null ? TagScheduleEvent.BuildTagData(e.Device, e.Tag, e.IsErrorNumberChanged) : e;

            var message = evt.ToEventMessage(isInitializationData);

            await _mqttClient.EnqueueAsync(new MqttApplicationMessageBuilder()
                .WithTopic(Options.GetTagScheduleEventsTopic(e.Device.DeviceId, e.Tag.TagId))
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .WithPayload(_messageSerializer.SerializeMessage(message))
                .WithRetainFlag(true)
                .Build()
            );
        }
    }
}
