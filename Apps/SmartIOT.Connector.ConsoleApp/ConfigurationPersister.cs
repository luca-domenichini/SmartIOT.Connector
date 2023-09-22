using SmartIOT.Connector.Core;
using SmartIOT.Connector.RestApi.Services;
using System.Text.Json;

namespace SmartIOT.Connector.ConsoleApp
{
    internal class ConfigurationPersister : IConfigurationPersister
    {
        private AppConfiguration _appConfiguration;
        private readonly string _configFilePath;

        public ConfigurationPersister(AppConfiguration appConfiguration, string configFilePath)
        {
            _appConfiguration = appConfiguration;
            _configFilePath = configFilePath;
        }

        public void PersistConfiguration(SmartIotConnectorConfiguration configuration)
        {
            _appConfiguration.Configuration = configuration;

            File.WriteAllText(_configFilePath, JsonSerializer.Serialize(_appConfiguration, new JsonSerializerOptions()
            {
                WriteIndented = true
            }));
        }
    }
}
