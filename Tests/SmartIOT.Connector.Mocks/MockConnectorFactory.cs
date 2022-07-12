using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Factory;

namespace SmartIOT.Connector.Mocks
{
	public class MockConnectorFactory : IConnectorFactory
	{
		public IConnector? CreateConnector(string connectionString)
		{
			if (connectionString.StartsWith("mock://"))
				return new MockConnector();

			return null;
		}
	}
}
