namespace SmartIOT.Connector.Core.Factory
{
    public class ConnectorFactory : IConnectorFactory
    {
        private readonly List<IConnectorFactory> _factories = new List<IConnectorFactory>();

        public void Add(IConnectorFactory factory)
        {
            _factories.Add(factory);
        }

        public void AddRange(IList<IConnectorFactory> connectorFactories)
        {
            _factories.AddRange(connectorFactories);
        }

        public bool Any(Func<IConnectorFactory, bool> predicate)
        {
            return _factories.Any(x => predicate(x));
        }

        public IConnector? CreateConnector(string connectionString)
        {
            return _factories.Select(x => x.CreateConnector(connectionString))
                .FirstOrDefault(x => x != null);
        }
    }
}
