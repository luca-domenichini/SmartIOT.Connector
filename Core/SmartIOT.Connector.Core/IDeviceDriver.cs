using SmartIOT.Connector.Core.Model;

namespace SmartIOT.Connector.Core;

public interface IDeviceDriver
{
    /// <summary>
    /// Questa property ritorna un nome descrittivo del driver, eventualmente basato sui device in esso contenuti
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// The device managed by this driver
    /// </summary>
		public Device Device { get; }

    /// <summary>
    /// Metodo di start generale dell'interfaccia del driver
    /// </summary>
    public int StartInterface();

    /// <summary>
    /// Metodo di stop generale dell'interfaccia del driver
    /// </summary>
    public int StopInterface();

    /// <summary>
    /// Metodo di connessione al singolo device
    /// </summary>
    public int Connect(Device device);

    /// <summary>
    /// Metodo di disconnessione dal singolo device
    /// </summary>
    public int Disconnect(Device device);

    /// <summary>
    /// Questo metodo è usato per leggere il tag specificato.
    /// Il metodo deve popolare i dati sull'array data passato in argomento.
    /// Lo startOffset passato in argomento ha un valore assoluto: quindi se un tag inizia al byte 100
    /// e il driver intende leggere i byte dal 110 al 120 passerà come argomenti startOffset = 110, length = 11.
    /// Attenzione che l'array data contiene direttamente i dati del tag, quindi un tag che inizia al byte 100 di lunghezza 50 byte
    /// avrà comunque un array di soli 50 bytes!
    /// </summary>
    public int ReadTag(Device device, Tag tag, byte[] data, int startOffset, int length);

    /// <summary>
    /// Questo metodo è usato per scrivere il tag specificato.
    /// Lo startOffset passato in argomento ha un valore assoluto: quindi se un tag inizia al byte 100
    /// e il driver intende scrivere i byte dal 110 al 120 passerà come argomenti startOffset = 110, length = 11.
    /// Attenzione che l'array data contiene direttamente i dati del tag, quindi un tag che inizia al byte 100 di lunghezza 50 byte
    /// avrà comunque un array di soli 50 bytes!
    /// </summary>
    public int WriteTag(Device device, Tag tag, byte[] data, int startOffset, int length);

    /// <summary>
    /// Questo metodo viene utilizzato per ottenere una rappresentaziona stringa del codice di errore riportato dalle chiamate
    /// ai metodi del driver che ritornano int come valore di ritorno. Tutti i valori di ritorno delle chiamate ai metodi del driver
    /// hanno un valore == 0 se il metodo ha avuto successo o un valore != 0 se si è verificato un errore.
    /// </summary>
    public string GetErrorMessage(int errorNumber);

    /// <summary>
    /// Questo metodo è richiamato per ottenere una rappresentazione string del device passato in argomento. Il metodo
    /// può invocare un metodo messo a disposizione da una libreria per ottenere le informazioni di cui necessita per recuperare
    /// i dati da visualizzare per poter descrivere il device (ad esempio, una cpu S7 313 ritorna un valore simile a 6ES7 313-5BG04-0AB0)
    /// </summary>
    public string GetDeviceDescription(Device device);
}
