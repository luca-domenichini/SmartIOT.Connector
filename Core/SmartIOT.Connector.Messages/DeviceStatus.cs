using ProtoBuf;

namespace SmartIOT.Connector.Messages;

public enum DeviceStatus
{
    [ProtoEnum] UNINITIALIZED,
    [ProtoEnum] OK,
    [ProtoEnum] ERROR
}
