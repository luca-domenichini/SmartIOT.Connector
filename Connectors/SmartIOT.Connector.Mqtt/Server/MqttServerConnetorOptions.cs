using SmartIOT.Connector.Messages.Serializers;

namespace SmartIOT.Connector.Mqtt.Server;

public class MqttServerConnectorOptions : MqttConnectorOptions
{
    public string ServerId { get; }
    public int ServerPort { get; }
    public bool IsPublishPartialReads { get; }

    public MqttServerConnectorOptions(string connectionString, bool isPublishWriteEvents, ISingleMessageSerializer messageSerializer, string serverId, int serverPort, string exceptionsTopicPattern, string deviceStatusEventsTopicPattern, string tagScheduleEventsTopicPattern, string tagWriteRequestCommandsTopicRoot, bool isPublishPartialReads)
        : base(connectionString, isPublishWriteEvents, messageSerializer, exceptionsTopicPattern, deviceStatusEventsTopicPattern, tagScheduleEventsTopicPattern, tagWriteRequestCommandsTopicRoot)
    {
        ServerId = serverId;
        ServerPort = serverPort;
        IsPublishPartialReads = isPublishPartialReads;
    }
}
