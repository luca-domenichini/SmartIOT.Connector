using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.RestApi.Model;

namespace SmartIOT.Connector.RestApi.Services;

public interface IDeviceService
{
    public IList<DeviceConfiguration> GetDeviceConfigurations();

    public DeviceConfiguration? GetDeviceConfiguration(string deviceId);

    public IList<Device> GetDevices();

    public Device? GetDevice(string deviceId);

    public Task AddDeviceAsync(DeviceConfiguration deviceConfiguration);

    public Task RemoveDeviceAsync(string deviceId);

    public TagData? GetTagData(string deviceId, string tagId);

    public void SetTagData(string deviceId, string tagId, TagData tagData);

    public Tag? GetTag(string deviceId, string tagId);

    public TagConfiguration? GetTagConfiguration(string deviceId, string tagId);

    public void AddTag(string deviceId, TagConfiguration tagConfiguration);

    public void RemoveTag(string deviceId, string tagId);

    public void UpdateTag(string deviceId, TagConfiguration tagConfiguration);
}
