namespace SmartIOT.Connector.RestApi.Services
{
	public interface IConnectorService
	{
		Model.Connector? AddConnector(string connectionString);
		bool ReplaceConnector(int id, string connectionString);
		bool DeleteConnector(int id);
	}
}
