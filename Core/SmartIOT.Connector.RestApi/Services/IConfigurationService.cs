using SmartIOT.Connector.Core;

namespace SmartIOT.Connector.RestApi.Services
{
    public interface IConfigurationService
    {
        public SmartIotConnectorConfiguration GetConfiguration();

        public void SaveConfiguration();
    }
}
