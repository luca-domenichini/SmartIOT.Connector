using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Factory;

namespace SmartIOT.Connector.Plc.Snap7
{
	public class Snap7DriverFactory : IDeviceDriverFactory
	{
		public IDictionary<DeviceConfiguration, IDeviceDriver> CreateDrivers(IList<DeviceConfiguration> deviceConfigurations)
		{
			return deviceConfigurations.Where(x => AcceptConnectionString(x.ConnectionString))
				.ToDictionary(k => k, v => (IDeviceDriver) CreateDriver(v));
		}

		private static bool AcceptConnectionString(string connectionString)
		{
			return connectionString?.ToLower().StartsWith("snap7://") ?? false;
		}

		private static Snap7Driver CreateDriver(DeviceConfiguration deviceConfiguration)
		{
			return new Snap7Driver(new Snap7Plc(new Snap7PlcConfiguration(deviceConfiguration)));
		}

	}
}
