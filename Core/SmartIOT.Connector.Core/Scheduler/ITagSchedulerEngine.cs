using SmartIOT.Connector.Core.Events;
using SmartIOT.Connector.Core.Model;

namespace SmartIOT.Connector.Core.Scheduler;

public interface ITagSchedulerEngine
{
    /// <summary>
    /// Driver di comunicazione con i device
    /// </summary>
    IDeviceDriver DeviceDriver { get; }

    /// <summary>
    /// Returns the managed device
    /// </summary>
    Device Device => DeviceDriver.Device;

    /// <summary>
    /// Questo metodo ritorna true se un tag è stato schedulato, altrimenti ritorna false valorizzando in <paramref name="waitTime"/>
    /// il tempo di attesa da applicare prima della prossima interrogazione.
    /// </summary>
    bool TryScheduleNextTag(bool scheduleWritesOnly, out TagSchedule schedule, out TimeSpan waitTime);

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
}
