namespace SmartIOT.Connector.Core.Factory
{
	public interface IConnectorFactory
	{
		/// <summary>
		/// Questa interfaccia rappresenta una factory dei connector con il quale avviene la comunicazione
		/// con sitemi esterni da parte del modulo driver.
		/// </summary>
		/// <returns>Il metodo ritorna un IConnector oppure null se non è in grado di manipolare la connectionString indicata.</returns>
		public IConnector? CreateConnector(string connectionString);
	}
}
