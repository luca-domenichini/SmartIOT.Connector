using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Factory;

namespace SmartIOT.Connector.Plc.SnapModBus;

public class SnapModBusDriverFactory : IDeviceDriverFactory
{
    private static bool AcceptConnectionString(string connectionString)
    {
        return connectionString?.ToLower().StartsWith("snapmodbus://") ?? false;
    }

    public IDeviceDriver? CreateDriver(DeviceConfiguration deviceConfiguration)
    {
        if (AcceptConnectionString(deviceConfiguration.ConnectionString))
            return new SnapModBusDriver(new SnapModBusNode(new SnapModBusNodeConfiguration(deviceConfiguration)));

        return null;
    }
}
