using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Factory;

namespace SmartIOT.Connector.Plc.Snap7;

public class Snap7DriverFactory : IDeviceDriverFactory
{
    private static bool AcceptConnectionString(string connectionString)
    {
        return connectionString?.StartsWith("snap7://", StringComparison.InvariantCultureIgnoreCase) ?? false;
    }

    public IDeviceDriver? CreateDriver(DeviceConfiguration deviceConfiguration)
    {
        if (AcceptConnectionString(deviceConfiguration.ConnectionString))
            return new Snap7Driver(new Snap7Plc(new Snap7PlcConfiguration(deviceConfiguration)));

        return null;
    }
}
