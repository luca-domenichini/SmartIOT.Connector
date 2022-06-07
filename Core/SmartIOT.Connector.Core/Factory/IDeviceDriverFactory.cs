using SmartIOT.Connector.Core.Conf;

namespace SmartIOT.Connector.Core.Factory
{
	public interface IDeviceDriverFactory
	{
		/// <summary>
		/// Questo metodo viene invocato per consentire al builder di construire i driver dei device
		/// indicati in argomento.
		/// Se un builder non gestisce le configurazioni passate non deve includere quel device nel dictionary di ritorno.
		/// </summary>
		IDictionary<DeviceConfiguration, IDeviceDriver> CreateDrivers(IList<DeviceConfiguration> deviceConfigurations);
	}
}
