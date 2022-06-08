using SmartIOT.Connector.Core.Events;

namespace SmartIOT.Connector.Core
{
	/// <summary>
	/// Questa delegate rappresenta la funzione che deve essere invocata nel momento in cui l'handler dello scheduler
	/// si connette con successo all'host remoto. In questo modo si ha la possibilità di inviare lo stato ultimo dei tag e dei device.
	/// </summary>
	/// <param name="initAction">Azione eseguita per tutti gli scheduler registrati sul modulo driver</param>
	/// <param name="afterInitAction">Azione eseguita una volta sola al termine dell'esecuzione della initAction</param>
	public delegate void InitializationActionDelegate(Action<IList<DeviceStatusEvent>, IList<TagScheduleEvent>> initAction, Action afterInitAction);

	/// <summary>
	/// Questa delegate rappresenta la funzione che deve essere invocata nel momento in cui il connector dello scheduler
	/// intende scrivere dati su un certo tag.
	/// </summary>
	public delegate void RequestTagWriteDelegate(string deviceId, string tagId, int startOffset, byte[] data);

	public delegate void ConnectorConnectedDelegate(IConnector connector, string info);

	public delegate void ConnectorDisconnectedDelegate(IConnector connector, string info);

	public class ConnectorInterface
	{
		public InitializationActionDelegate InitializationActionDelegate { get; }
		public RequestTagWriteDelegate RequestTagWriteDelegate { get; }
		public ConnectorConnectedDelegate ConnectedDelegate { get; }
		public ConnectorDisconnectedDelegate DisconnectedDelegate { get; }

		public ConnectorInterface(InitializationActionDelegate initializationActionDelegate, RequestTagWriteDelegate requestDataWriteDelegate, ConnectorConnectedDelegate connectedDelegate, ConnectorDisconnectedDelegate disconnectedDelegate)
		{
			InitializationActionDelegate = initializationActionDelegate;
			RequestTagWriteDelegate = requestDataWriteDelegate;
			ConnectedDelegate = connectedDelegate;
			DisconnectedDelegate = disconnectedDelegate;
		}
	}
}
