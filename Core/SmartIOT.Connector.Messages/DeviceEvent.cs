using ProtoBuf;

namespace SmartIOT.Connector.Messages;

[ProtoContract]
public class DeviceEvent
{
    [ProtoMember(1)]
    public string DeviceId { get; set; } = string.Empty;

    [ProtoMember(2)]
    public DeviceStatus DeviceStatus { get; set; }

    [ProtoMember(3)]
    public int ErrorNumber { get; set; }

    [ProtoMember(4)]
    public string Description { get; set; } = string.Empty;

    public DeviceEvent()
    {
    }

    public DeviceEvent(string deviceId, DeviceStatus deviceStatus, int errorNumber, string description)
    {
        DeviceId = deviceId;
        DeviceStatus = deviceStatus;
        ErrorNumber = errorNumber;
        Description = description;
    }

    public override string? ToString()
    {
        return $"[{nameof(DeviceEvent)}] Device {DeviceId}, DeviceStatus {Enum.GetName(typeof(DeviceStatus), DeviceStatus)}, Error: {ErrorNumber} '{Description}'";
    }
}