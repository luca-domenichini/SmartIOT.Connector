using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Factory;

namespace SmartIOT.Connector.Mocks;

public class FakeConnectorFactory : IConnectorFactory
{
    private readonly IServiceProvider? _serviceProvider;

    public FakeConnectorFactory()
    {
    }

    // contructor to test DI injection
    public FakeConnectorFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IConnector? CreateConnector(string connectionString)
    {
        if (connectionString.StartsWith("fake://"))
            return new FakeConnector(_serviceProvider!);

        return null;
    }
}
