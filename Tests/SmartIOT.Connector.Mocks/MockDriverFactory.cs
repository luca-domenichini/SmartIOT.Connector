using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Factory;

namespace SmartIOT.Connector.Mocks;

public class MockDriverFactory : IDeviceDriverFactory
{
    private static bool AcceptConnectionString(string connectionString)
    {
        return connectionString?.StartsWith("mock://", StringComparison.InvariantCultureIgnoreCase) ?? false;
    }

    public IDeviceDriver? CreateDriver(DeviceConfiguration deviceConfiguration)
    {
        if (AcceptConnectionString(deviceConfiguration.ConnectionString))
            return new MockDeviceDriver(new Core.Model.Device(new DeviceConfiguration(deviceConfiguration)));

        return null;
    }
}
