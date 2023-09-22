using SmartIOT.Connector.Core.Model;

namespace SmartIOT.Connector.RestApi.Model;

public class Device
{
    /// <summary>
    /// The device Id
    /// </summary>
    public string DeviceId { get; }

    /// <summary>
    /// The device status
    /// </summary>
    public DeviceStatus DeviceStatus { get; }

    /// <summary>
    /// The list of tags managed by this device
    /// </summary>
    public IList<Tag> Tags { get; }

    public Device(string deviceId, DeviceStatus deviceStatus, IList<Tag> tags)
    {
        DeviceId = deviceId;
        DeviceStatus = deviceStatus;
        Tags = tags;
    }
}
