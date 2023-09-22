using MQTTnet;
using MQTTnet.Server;
using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Connector;
using SmartIOT.Connector.Core.Events;
using SmartIOT.Connector.Messages;
using SmartIOT.Connector.Messages.Serializers;

namespace SmartIOT.Connector.Mqtt.Server;

public class MqttServerConnector : AbstractPublisherConnector
{
    private new MqttServerConnectorOptions Options => (MqttServerConnectorOptions)base.Options;
    private readonly MqttServer _mqttServer;
    private readonly ISingleMessageSerializer _messageSerializer;
    private bool _started;

    public MqttServerConnector(MqttServerConnectorOptions options)
        : base(options)
    {
        _messageSerializer = options.MessageSerializer;

        _mqttServer = new MqttFactory().CreateMqttServer(new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(Options.ServerPort)
            .Build());

        _mqttServer.ClientConnectedAsync += OnClientConnected;
        _mqttServer.ClientDisconnectedAsync += OnClientDisconnected;
        _mqttServer.InterceptingPublishAsync += OnApplicationMessageReceived;

        _mqttServer.ClientSubscribedTopicAsync += OnClientSubscribedTopic;

        _mqttServer.StartedAsync += OnStarted;
        _mqttServer.StoppedAsync += OnStopped;
    }

    private async Task OnClientSubscribedTopic(ClientSubscribedTopicEventArgs e)
    {
        if (Options.IsDeviceStatusEventsTopicRoot(e.TopicFilter.Topic))
        {
            await InvokeInitializationDelegateAsync(true, false);
        }
        if (Options.IsTagScheduleEventsTopicRoot(e.TopicFilter.Topic))
        {
            await InvokeInitializationDelegateAsync(false, true);
        }
    }

    public override async Task StartAsync(ISmartIOTConnectorInterface connectorInterface)
    {
        await base.StartAsync(connectorInterface);

        await _mqttServer.StartAsync();
    }

    private Task OnStarted(EventArgs obj)
    {
        _started = true;
        return Task.CompletedTask;
    }

    public override async Task StopAsync()
    {
        await base.StopAsync();

        await _mqttServer.StopAsync();
    }

    private Task OnStopped(EventArgs obj)
    {
        _started = false;
        return Task.CompletedTask;
    }

    private Task OnApplicationMessageReceived(InterceptingPublishEventArgs e)
    {
        if (e.ApplicationMessage.Topic.StartsWith(Options.TagWriteRequestCommandsTopicRoot, StringComparison.InvariantCultureIgnoreCase))
        {
            var command = _messageSerializer.DeserializeMessage<TagWriteRequestCommand>(e.ApplicationMessage.PayloadSegment.Array!);
            if (command != null)
                ConnectorInterface!.RequestTagWrite(command.DeviceId, command.TagId, command.StartOffset, command.Data);
        }

        return Task.CompletedTask;
    }

    private Task OnClientConnected(ClientConnectedEventArgs e)
    {
        ConnectorInterface!.OnConnectorConnected(new ConnectorConnectedEventArgs(this, $"ClientId {e.ClientId} connected to port {Options.ServerPort}"));
        return Task.CompletedTask;
    }

    private Task OnClientDisconnected(ClientDisconnectedEventArgs e)
    {
        ConnectorInterface!.OnConnectorDisconnected(new ConnectorDisconnectedEventArgs(this, $"ClientId {e.ClientId} disconnected: {e.DisconnectType}"));
        return Task.CompletedTask;
    }

    protected override async Task PublishExceptionAsync(Exception exception)
    {
        if (_started)
        {
            try
            {
                await _mqttServer.InjectApplicationMessage(new InjectedMqttApplicationMessage(
                    new MqttApplicationMessageBuilder()
                        .WithTopic(Options.ExceptionsTopicPattern)
                        .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                        .WithPayload(_messageSerializer.SerializeMessage(EventExtensions.ToEventMessage(exception)))
                        .Build())
                );
            }
            catch (Exception ex)
            {
                OnException(ex);
            }
        }
    }

    protected override async Task PublishDeviceStatusEventAsync(DeviceStatusEvent e)
    {
        if (_started)
        {
            try
            {
                await _mqttServer.InjectApplicationMessage(new InjectedMqttApplicationMessage(
                    new MqttApplicationMessageBuilder()
                        .WithTopic(Options.GetDeviceStatusEventsTopic(e.Device.DeviceId))
                        .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                        .WithPayload(_messageSerializer.SerializeMessage(EventExtensions.ToEventMessage(e)))
                        .Build())
                );
            }
            catch (Exception ex)
            {
                OnException(ex);
            }
        }
    }

    protected override async Task PublishTagScheduleEventAsync(TagScheduleEvent e)
    {
        await PublishTagScheduleEvent(e, false);
    }

    private async Task PublishTagScheduleEvent(TagScheduleEvent e, bool isInitializationData)
    {
        if (_started)
        {
            var evt = !Options.IsPublishPartialReads && e.Data != null ? TagScheduleEvent.BuildTagData(e.Device, e.Tag, e.IsErrorNumberChanged) : e;

            var message = evt.ToEventMessage(isInitializationData);

            try
            {
                await _mqttServer.InjectApplicationMessage(new InjectedMqttApplicationMessage(
                    new MqttApplicationMessageBuilder()
                        .WithTopic(Options.GetTagScheduleEventsTopic(e.Device.DeviceId, e.Tag.TagId))
                        .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                        .WithPayload(_messageSerializer.SerializeMessage(message))
                        .Build())
                );
            }
            catch (Exception ex)
            {
                OnException(ex);
            }
        }
    }

    private async Task InvokeInitializationDelegateAsync(bool publishDeviceStatusEvents, bool publishTagScheduleEvents)
    {
        await ConnectorInterface!.RunInitializationActionAsync(
            initAction: async (deviceEvents, tagEvents) =>
            {
                if (publishDeviceStatusEvents)
                {
                    foreach (var deviceEvent in deviceEvents)
                    {
                        await PublishDeviceStatusEventAsync(deviceEvent);
                    }
                }
                if (publishTagScheduleEvents)
                {
                    foreach (var tagEvent in tagEvents)
                    {
                        await PublishTagScheduleEvent(tagEvent, true);
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
