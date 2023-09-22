using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Factory;

namespace SmartIOT.Connector.Mocks
{
    public class FakeConnectorFactory : IConnectorFactory
    {
        public IConnector? CreateConnector(string connectionString)
        {
            if (connectionString.StartsWith("fake://"))
                return new FakeConnector();

            return null;
        }
    }
}
