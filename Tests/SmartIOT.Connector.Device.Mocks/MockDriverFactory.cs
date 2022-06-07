using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Factory;

namespace SmartIOT.Connector.Device.Mocks
{
	public class MockDriverFactory : IDeviceDriverFactory
	{
		public IDictionary<DeviceConfiguration, IDeviceDriver> CreateDrivers(IList<DeviceConfiguration> deviceConfigurations)
		{
			return deviceConfigurations.Where(x => AcceptConnectionString(x.ConnectionString))
				.ToDictionary(k => k, v => (IDeviceDriver) CreateDriver(v));
		}

		private static bool AcceptConnectionString(string connectionString)
		{
			return connectionString?.ToLower().StartsWith("mock://") ?? false;
		}

		private static MockDeviceDriver CreateDriver(DeviceConfiguration deviceConfiguration)
		{
			return new MockDeviceDriver(new Core.Model.Device(new DeviceConfiguration(deviceConfiguration)));
		}

	}
}
