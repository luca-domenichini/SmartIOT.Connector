using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Factory;

namespace SmartIOT.Connector.RestApi.Services
{
	public class ConnectorService : IConnectorService
	{
        private readonly SmartIotConnector _smartiotConnector;
		private readonly IConnectorFactory _connectorFactory;

		public ConnectorService(SmartIotConnector smartiotConnector, IConnectorFactory connectorFactory)
		{
			_smartiotConnector = smartiotConnector;
			_connectorFactory = connectorFactory;
		}

		public Model.Connector? AddConnector(string connectionString)
		{
			var connector = _connectorFactory.CreateConnector(connectionString);
			if (connector == null)
				return null;
			
			int index = _smartiotConnector.AddConnector(connector);

			return new Model.Connector(index, connectionString);
		}

		public bool DeleteConnector(int id)
		{
			return _smartiotConnector.RemoveConnector(id);
		}

		public bool ReplaceConnector(int id, string connectionString)
		{
			var connector = _connectorFactory.CreateConnector(connectionString);
			if (connector == null)
				return false;

			return _smartiotConnector.ReplaceConnector(id, connector);
		}
	}
}
