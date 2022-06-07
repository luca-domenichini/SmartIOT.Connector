using SmartIOT.Connector.Core.Events;
using SmartIOT.Connector.Core.Model;

namespace SmartIOT.Connector.Core.Scheduler
{
	public interface ITagSchedulerEngine
	{
		/// <summary>
		/// Driver di comunicazione con i device
		/// </summary>
		IDeviceDriver DeviceDriver { get; }

		/// <summary>
		/// Questo metodo ritorna il prossimo tag da schedulare, oppure tira TagSchedulerWaitException se non c'è nulla da schedulare.
		/// Il metodo esegue la schedulazione del tag che ha ritornato.
		/// Il parametro scheduleWritesOnly indica se schedulare solo tag in scrittura e non procedere oltre con nuove letture. Questo parametro
		/// è usato per terminare l'engine e evitare di leggere nuovi dati.
		/// </summary>
		TagSchedule ScheduleNextTag(bool scheduleWritesOnly);

		/// <summary>
		/// Questo metodo effettua la ripartenza del driver se deve essere fatto ripartire perché è stato superato il numero massimo
		/// di errori consecutivi.
		/// </summary>
		void RestartDriver();

		/// <summary>
		/// Restarting event
		/// </summary>
		public event EventHandler<DeviceDriverRestartingEventArgs>? RestartingEvent;

		/// <summary>
		/// Restart completed event
		/// </summary>
		public event EventHandler<DeviceDriverRestartedEventArgs>? RestartedEvent;

		/// <summary>
		/// Evento rilanciato quando vieen letto un tag
		/// </summary>
		event EventHandler<TagScheduleEventArgs>? TagReadEvent;
		/// <summary>
		/// Evento rilanciato quanto viene scritto un tag
		/// </summary>
		event EventHandler<TagScheduleEventArgs>? TagWriteEvent;
		/// <summary>
		/// Evento rilanciato quando si verifica un cambiato di stato di un device
		/// </summary>
		event EventHandler<DeviceStatusEventArgs>? DeviceStatusEvent;
		/// <summary>
		/// Evento rilanciato quando si verifica una eccezione non gestita durante l'invocazione degli altri eventi
		/// </summary>
		event EventHandler<ExceptionEventArgs>? ExceptionHandler;

		/// <summary>
		/// Ritorna l'elenco dei device gestiti dall'engine
		/// </summary>
		IList<Device> GetManagedDevices();
	}
}
