using SmartIOT.Connector.Core.Conf;

namespace SmartIOT.Connector.Core.Factory
{
    /// <summary>
    /// This interface represents a factory that builds an IDeviceDriver for a provided DeviceConfiguration
    /// </summary>
    public interface IDeviceDriverFactory
    {
        public IDeviceDriver? CreateDriver(DeviceConfiguration deviceConfiguration);
    }
}
