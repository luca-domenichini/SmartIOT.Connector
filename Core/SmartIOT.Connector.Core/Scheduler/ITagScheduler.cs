using SmartIOT.Connector.Core.Events;
using SmartIOT.Connector.Core.Model;

namespace SmartIOT.Connector.Core.Scheduler
{
	public interface ITagScheduler
	{
		public event EventHandler<DeviceDriverRestartingEventArgs>? EngineRestartingEvent;
		public event EventHandler<DeviceDriverRestartedEventArgs>? EngineRestartedEvent;
		public event EventHandler<TagSchedulerWaitExceptionEventArgs>? TagSchedulerWaitExceptionEvent;
		public event EventHandler<TagScheduleEventArgs>? TagReadEvent;
		public event EventHandler<TagScheduleEventArgs>? TagWriteEvent;
		public event EventHandler<DeviceStatusEventArgs>? DeviceStatusEvent;
		public event EventHandler<ExceptionEventArgs>? ExceptionHandler;
		public bool IsPaused { get; set; }
		public IDeviceDriver DeviceDriver { get; }

		void Start();
		void Stop();
		void AddConnector(IConnector connector);
		void RemoveConnector(IConnector connector);
		IList<Device> GetManagedDevices();

		/// <summary>
		/// Questo metodo consente di eseguire una action di inizializzazione,
		/// consentendo di inviare lo stato dei device e dei tag a un server remoto.
		/// </summary>
		void RunInitializationAction(Action<IList<DeviceStatusEvent>, IList<TagScheduleEvent>> initializationAction);
	}
}
