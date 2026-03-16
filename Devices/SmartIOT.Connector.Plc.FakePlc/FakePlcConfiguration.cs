using SmartIOT.Connector.Core.Conf;

namespace SmartIOT.Connector.Plc.FakePlc;

/// <summary>
/// Configuration for a FakePlc device.  The connection string must start with "fakeplc://".
/// Example: "fakeplc://device1"
/// </summary>
public class FakePlcConfiguration : DeviceConfiguration
{
    public FakePlcConfiguration(DeviceConfiguration configuration) : base(configuration)
    {
    }

    public FakePlcConfiguration(string connectionString, string deviceId, string name, bool enabled = true)
    {
        ConnectionString = connectionString;
        DeviceId = deviceId;
        Name = name;
        Enabled = enabled;
    }
}
