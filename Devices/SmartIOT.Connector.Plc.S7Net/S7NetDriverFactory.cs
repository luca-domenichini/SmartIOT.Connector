using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Factory;

namespace SmartIOT.Connector.Plc.S7Net
{
	public class S7NetDriverFactory : IDeviceDriverFactory
	{
		public IDictionary<DeviceConfiguration, IDeviceDriver> CreateDrivers(IList<DeviceConfiguration> deviceConfigurations)
		{
			return deviceConfigurations.Where(x => AcceptConnectionString(x.ConnectionString))
				.ToDictionary(k => k, v => (IDeviceDriver)CreateDriver(v));
		}

		private static bool AcceptConnectionString(string connectionString)
		{
			return connectionString?.ToLower().StartsWith("s7net://") ?? false;
		}

		private static S7NetDriver CreateDriver(DeviceConfiguration deviceConfiguration)
		{
			return new S7NetDriver(new S7NetPlc(new S7NetPlcConfiguration(deviceConfiguration)));
		}

	}
}
