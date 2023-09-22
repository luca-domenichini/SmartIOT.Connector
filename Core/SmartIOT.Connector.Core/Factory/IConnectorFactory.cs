namespace SmartIOT.Connector.Core.Factory;

/// <summary>
/// This interface represents a connector factory used to create the connector to communicate with external systems.
/// </summary>
public interface IConnectorFactory
{
    /// <summary>
    /// Method used to create the IConnector for a given connectionString.
    /// </summary>
    /// <returns>The method returns an IConnector or null if this factory is not able to create an IConnector for the provided connectionString</returns>
    public IConnector? CreateConnector(string connectionString);
}
