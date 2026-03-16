using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Factory;

namespace SmartIOT.Connector.Plc.FakePlc;

/// <summary>
/// Factory that creates <see cref="FakeDriver"/> instances for connection strings starting
/// with <c>fakeplc://</c>.
/// </summary>
public class FakeDriverFactory : IDeviceDriverFactory
{
    private static bool AcceptsConnectionString(string connectionString) =>
        connectionString?.StartsWith("fakeplc://", StringComparison.OrdinalIgnoreCase) ?? false;

    public IDeviceDriver? CreateDriver(DeviceConfiguration deviceConfiguration)
    {
        if (!AcceptsConnectionString(deviceConfiguration.ConnectionString))
            return null;

        var config = new FakePlcConfiguration(deviceConfiguration);
        var device = new FakePlcDevice(config);
        return new FakeDriver(device);
    }
}
