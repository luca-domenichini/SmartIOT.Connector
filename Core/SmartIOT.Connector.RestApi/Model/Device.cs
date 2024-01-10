using SmartIOT.Connector.Core.Model;
using Swashbuckle.AspNetCore.Annotations;

namespace SmartIOT.Connector.RestApi.Model;

public class Device
{
    /// <summary>
    /// The device Id
    /// </summary>
    [SwaggerSchema("The device Id", Nullable = false)]
    public string DeviceId { get; }

    /// <summary>
    /// The device status
    /// </summary>
    [SwaggerSchema("The device status", Nullable = false)]
    public DeviceStatus DeviceStatus { get; }

    /// <summary>
    /// The list of tags managed by this device
    /// </summary>
    [SwaggerSchema("The list of tags managed by this device", Nullable = false)]
    public IList<Tag> Tags { get; }

    public Device(string deviceId, DeviceStatus deviceStatus, IList<Tag> tags)
    {
        DeviceId = deviceId;
        DeviceStatus = deviceStatus;
        Tags = tags;
    }
}
