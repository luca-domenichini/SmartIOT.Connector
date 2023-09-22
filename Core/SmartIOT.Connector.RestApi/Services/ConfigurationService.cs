using SmartIOT.Connector.Core;

namespace SmartIOT.Connector.RestApi.Services;

public class ConfigurationService : IConfigurationService
{
    private readonly SmartIotConnector _smartIotConnector;
    private readonly IConfigurationPersister _configurationPersister;

    public ConfigurationService(SmartIotConnector smartIotConnector, IConfigurationPersister configurationPersister)
    {
        _smartIotConnector = smartIotConnector;
        _configurationPersister = configurationPersister;
    }

    public SmartIotConnectorConfiguration GetConfiguration()
    {
        return new SmartIotConnectorConfiguration
        {
            ConnectorConnectionStrings = _smartIotConnector.Connectors.Select(x => x.ConnectionString).ToList(),
            DeviceConfigurations = _smartIotConnector.Schedulers.Select(x => x.Device.Configuration).ToList(),
            SchedulerConfiguration = _smartIotConnector.SchedulerConfiguration
        };
    }

    public void SaveConfiguration()
    {
        _configurationPersister.PersistConfiguration(GetConfiguration());
    }
}
