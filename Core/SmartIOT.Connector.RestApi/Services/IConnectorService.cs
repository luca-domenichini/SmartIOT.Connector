namespace SmartIOT.Connector.RestApi.Services
{
    public interface IConnectorService
    {
        Task<Model.Connector?> AddConnectorAsync(string connectionString);

        Task<bool> ReplaceConnectorAsync(int id, string connectionString);

        Task<bool> DeleteConnectorAsync(int id);
    }
}
