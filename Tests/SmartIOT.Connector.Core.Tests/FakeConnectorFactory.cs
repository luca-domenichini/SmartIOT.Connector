using SmartIOT.Connector.Core.Factory;

namespace SmartIOT.Connector.Core.Tests
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
