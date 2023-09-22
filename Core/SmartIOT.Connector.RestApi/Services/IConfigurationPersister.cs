using SmartIOT.Connector.Core;

namespace SmartIOT.Connector.RestApi.Services;

/// <summary>
/// This interface a the callback used by ConfigurationService to invoke the overall saving of the configuration
/// </summary>
public interface IConfigurationPersister
{
    public void PersistConfiguration(SmartIotConnectorConfiguration configuration);
}
