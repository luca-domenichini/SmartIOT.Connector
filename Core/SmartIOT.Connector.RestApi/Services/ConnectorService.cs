using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Factory;

namespace SmartIOT.Connector.RestApi.Services;

public class ConnectorService : IConnectorService
{
    private readonly SmartIotConnector _smartiotConnector;
    private readonly IConnectorFactory _connectorFactory;

    public ConnectorService(SmartIotConnector smartiotConnector, IConnectorFactory connectorFactory)
    {
        _smartiotConnector = smartiotConnector;
        _connectorFactory = connectorFactory;
    }

    public async Task<Model.Connector?> AddConnectorAsync(string connectionString)
    {
        var connector = _connectorFactory.CreateConnector(connectionString);
        if (connector == null)
            return null;

        int index = await _smartiotConnector.AddConnectorAsync(connector);

        return new Model.Connector(index, connectionString);
    }

    public Task<bool> DeleteConnectorAsync(int id)
    {
        return _smartiotConnector.RemoveConnectorAsync(id);
    }

    public async Task<bool> ReplaceConnectorAsync(int id, string connectionString)
    {
        var connector = _connectorFactory.CreateConnector(connectionString);
        if (connector == null)
            return false;

        return await _smartiotConnector.ReplaceConnectorAsync(id, connector);
    }
}
