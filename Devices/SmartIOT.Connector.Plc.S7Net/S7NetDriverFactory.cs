using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Factory;

namespace SmartIOT.Connector.Plc.S7Net;

public class S7NetDriverFactory : IDeviceDriverFactory
{
    private static bool AcceptConnectionString(string connectionString)
    {
        return connectionString?.StartsWith("s7net://", StringComparison.InvariantCultureIgnoreCase) ?? false;
    }

    public IDeviceDriver? CreateDriver(DeviceConfiguration deviceConfiguration)
    {
        if (AcceptConnectionString(deviceConfiguration.ConnectionString))
            return new S7NetDriver(new S7NetPlc(new S7NetPlcConfiguration(deviceConfiguration)));

        return null;
    }
}
