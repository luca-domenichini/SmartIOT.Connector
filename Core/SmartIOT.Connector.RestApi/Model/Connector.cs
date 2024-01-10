using Swashbuckle.AspNetCore.Annotations;

namespace SmartIOT.Connector.RestApi.Model;

public class Connector
{
    /// <summary>
    /// Index of the connector in SmartIOT.Connector list
    /// </summary>
    [SwaggerSchema("Index of the connector in SmartIOT.Connector list", Nullable = false)]
    public int Index { get; }

    /// <summary>
    /// ConnectionString with connector parameters
    /// </summary>
    [SwaggerSchema("ConnectionString with connector parameters", Nullable = false)]
    public string ConnectionString { get; }

    public Connector(int index, string connectionString)
    {
        Index = index;
        ConnectionString = connectionString;
    }
}
