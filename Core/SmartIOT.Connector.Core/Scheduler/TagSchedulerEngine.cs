using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Events;
using SmartIOT.Connector.Core.Model;
using SmartIOT.Connector.Core.Util;
using System.Diagnostics;

namespace SmartIOT.Connector.Core.Scheduler
{
    public class TagSchedulerEngine : ITagSchedulerEngine
    {
        private readonly ITimeService _timeService;
        private readonly SchedulerConfiguration _configuration;
        private DateTime? _lastRestartInstant;
        public IDeviceDriver DeviceDriver { get; }

        public event EventHandler<DeviceDriverRestartingEventArgs>? RestartingEvent;

        public event EventHandler<DeviceDriverRestartedEventArgs>? RestartedEvent;

        public event EventHandler<TagScheduleEventArgs>? TagReadEvent;

        public event EventHandler<TagScheduleEventArgs>? TagWriteEvent;

        public event EventHandler<DeviceStatusEventArgs>? DeviceStatusEvent;

        public event EventHandler<ExceptionEventArgs>? ExceptionHandler;

        public TagSchedulerEngine(IDeviceDriver deviceDriver, ITimeService timeService, SchedulerConfiguration configuration)
        {
            DeviceDriver = deviceDriver;
            _timeService = timeService;
            _configuration = configuration;
        }

        public TagSchedule ScheduleNextTag(bool scheduleWritesOnly)
        {
            lock (DeviceDriver)
            {
                var schedule = GetNextTagSchedule(scheduleWritesOnly);
                ScheduleTag(schedule);

                return schedule;
            }
        }

        public TagSchedule GetNextTagSchedule(bool scheduleWritesOnly = false)
        {
            Tag? tagToSchedule = null;
            TimeSpan waitTime = TimeSpan.FromSeconds(1);

            var device = DeviceDriver.Device;

            foreach (Tag tag in device.Tags)
            {
                if (scheduleWritesOnly && tag.TagType != TagType.WRITE)
                    continue;

                var time = GetRemainingTimeForTagSchedule(device, tag);
                if (time <= TimeSpan.Zero)
                {
                    // tag schedulabile! verifico se posso assegnarlo
                    if (tagToSchedule == null)
                    { // nessun tag ancora assegnato
                        tagToSchedule = tag;
                    }
                    else
                    {
                        // tag già assegnato
                        // il tag corrente e' in scrittura: cambio solo con 1 tag in scrittura che non viene scritto da piu' tempo
                        if (tagToSchedule.IsWriteSynchronizationRequested)
                        {
                            if (tag.IsWriteSynchronizationRequested && tag.TagType == TagType.WRITE && (tag.LastDeviceSynchronization < tagToSchedule.LastDeviceSynchronization))
                            {
                                tagToSchedule = tag;
                            }
                        }
                        // il tag corrente e' in lettura: cambio con un tag in scrittura da scrivere o uno in lettura che ha un numero inferiore di punti
                        else
                        {
                            if ((tag.IsWriteSynchronizationRequested && tag.TagType == TagType.WRITE)
                                    || (tag.TagType == TagType.READ && tag.Points < tagToSchedule.Points))
                            {
                                tagToSchedule = tag;
                            }
                        }
                    }
                }
                else
                {
                    if (time < waitTime)
                        waitTime = time;
                }
            }

            if (tagToSchedule != null)
                return new TagSchedule(device, tagToSchedule, tagToSchedule.TagType == TagType.READ ? TagScheduleType.READ : TagScheduleType.WRITE);

            // se non ho trovato nessun tag da schedulare, rilancio una eccezione indicando il tempo di attesa
            throw new TagSchedulerWaitException(waitTime);
        }

        public void ScheduleTag(TagSchedule schedule)
        {
            Device deviceToSchedule = schedule.Device;

            if (schedule.Type == TagScheduleType.READ)
            {
                // se le letture parziali sono abilitate, devo leggere una PDU alla volta
                // e inoltrare l'evento di lettura per ogni singola lettura parziale.
                // Dopo ogni lettura parziale devo controllare se ho un tag in scrittura da sincronizzare, in modo da dare la possibilità al controllore di scrivere subito.
                // Internamente al metodo ReadTag(), se la lettura è parziale verrà letta solo una parte del datablock
                // e il controllo torna allo scheduler, il quale schedulerà una scrittura se necessaria, oppure lo stesso datablock in lettura (dal momento che una lettura parziale non marca il tag come "synchronized").
                bool partialRead = deviceToSchedule.IsPartialReadsEnabled;

                if (partialRead)
                {
                    int singlePDUReadBytes = deviceToSchedule.SinglePDUReadBytes;
                    if (singlePDUReadBytes <= 0)
                        partialRead = false;
                }

                if (!partialRead)
                {
                    TagScheduleEvent? evt = ReadTag(schedule, false, false);
                    if (evt == null)
                        throw new InvalidOperationException("A complete read should always produce a TagRead event");

                    OnTagRead(evt);
                }
                else
                {
                    bool somethingToWrite = false;
                    TagScheduleEvent? evt;
                    do
                    {
                        evt = ReadTag(schedule, true, false);
                        if (evt != null)
                        {
                            // partial read complete
                            OnTagRead(evt);
                        }
                        else
                        {
                            // check for tag write only if partial read is not completed
                            foreach (Tag tag in DeviceDriver.Device.Tags)
                            {
                                if (tag.IsWriteSynchronizationRequested)
                                {
                                    var timeToWait = GetRemainingTimeForTagSchedule(DeviceDriver.Device, tag);
                                    if (timeToWait <= TimeSpan.Zero)
                                    {
                                        somethingToWrite = true;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    while (!somethingToWrite && evt == null);
                }
            }
            else // TagScheduleType.WRITE
            {
                TagScheduleEvent evt = WriteTag(schedule);
                OnTagWrite(evt);
            }
        }

        private TimeSpan GetRemainingTimeForTagSchedule(Device device, Tag tag)
        {
            if (device.DeviceStatus == DeviceStatus.OK
                && tag.IsInitialized
                && (tag.TagType == TagType.READ || tag.IsWriteSynchronizationRequested))
            {
                if (tag.ErrorCode == 0)
                {
                    var timePassedFromLastSync = _timeService.Now - tag.LastDeviceSynchronization;
                    if (timePassedFromLastSync < TimeSpan.Zero)
                    {
                        tag.LastDeviceSynchronization = new DateTime(0, DateTimeKind.Unspecified);
                        timePassedFromLastSync = _configuration.WaitTimeBetweenEverySchedule;
                    }

                    TimeSpan timeSpanAllSchedules = _configuration.WaitTimeBetweenEverySchedule - timePassedFromLastSync;
                    if (tag.TagType == TagType.WRITE && tag.IsWriteSynchronizationRequested)
                        return timeSpanAllSchedules;

                    TimeSpan timeSpanReadSchedules = _configuration.WaitTimeBetweenReadSchedules - timePassedFromLastSync;

                    if (timeSpanAllSchedules > timeSpanReadSchedules)
                        return timeSpanAllSchedules;

                    return timeSpanReadSchedules;
                }
                else
                {
                    var timePassedFromLastError = _timeService.Now - tag.LastErrorDateTime;
                    if (timePassedFromLastError < TimeSpan.Zero)
                    {
                        tag.LastErrorDateTime = new DateTime(0, DateTimeKind.Unspecified);
                        timePassedFromLastError = _configuration.WaitTimeAfterError;
                    }

                    TimeSpan timeSpanForError = _configuration.WaitTimeAfterError - timePassedFromLastError;
                    if (tag.TagType == TagType.WRITE && tag.IsWriteSynchronizationRequested)
                        return timeSpanForError;

                    TimeSpan timeSpanReadSchedules = _configuration.WaitTimeBetweenReadSchedules - timePassedFromLastError;

                    if (timeSpanForError > timeSpanReadSchedules)
                        return timeSpanForError;

                    return timeSpanReadSchedules;
                }
            }

            // in caso non trovo nessun tag da schedulare ritorno 1 secondo di default
            return TimeSpan.FromSeconds(1);
        }

        private TagScheduleEvent? ReadTag(TagSchedule schedule, bool partialRead, bool forInitialization)
        {
            TagScheduleEvent? evt;
            var device = schedule.Device;
            var tag = schedule.Tag;

            lock (tag)
            {
                var stopWatch = Stopwatch.StartNew();

                int err;
                string description = string.Empty;
                if (!partialRead)
                {
                    device.SetTagSynchronized(tag, _timeService.Now);
                    (err, description) = TryWithDeviceDriver(x => x.ReadTag(device, tag, tag.Data, tag.ByteOffset, tag.Size));
                }
                else
                {
                    int singlePDUReadBytes = device.SinglePDUReadBytes;
                    if (singlePDUReadBytes <= 0)
                        throw new InvalidOperationException("cannot do a partial read when singlePDUReadBytes <= 0");

                    int index = tag.CurrentReadIndex;
                    int length = Math.Min(tag.Size - index, singlePDUReadBytes);

                    (err, description) = TryWithDeviceDriver(x => x.ReadTag(device, tag, tag.Data, tag.ByteOffset + index, length));

                    if (err == 0)
                    {
                        index += length;
                        if (index >= tag.Size)
                            index = 0;

                        tag.CurrentReadIndex = index;
                    }
                }

                int oldError = tag.ErrorCode;
                tag.ErrorCode = err;
                bool errorStateChanged = oldError != err;

                var time = stopWatch.Elapsed;

                if (err == 0)
                {
                    int index = tag.CurrentReadIndex;
                    if (partialRead)
                    {
                        tag.PartialReadTotalTime += time;

                        long count = tag.PartialReadCount;
                        tag.PartialReadCount = count + 1;

                        if (count >= 100)
                            count = 99;

                        tag.PartialReadMeanTime = (tag.PartialReadMeanTime * count + time) / (count + 1);

                        if (index == 0)
                        {
                            // check for changes
                            evt = ParseTagChangesAndMerge(device, tag, errorStateChanged);

                            // quando index == 0 significa che il tag è stato letto tutto con lettura parziali consecutive
                            device.SetTagSynchronized(tag, _timeService.Now);

                            time = tag.PartialReadTotalTime;
                            tag.PartialReadTotalTime = TimeSpan.Zero;
                            tag.IsInitialized = true;
                        }
                        else
                        {
                            // incomplete partial-read: no event is raised
                            evt = null;
                        }
                    }
                    else
                    {
                        // check for changes
                        evt = ParseTagChangesAndMerge(device, tag, errorStateChanged);

                        tag.IsInitialized = true;
                    }

                    if (!forInitialization && (!partialRead || index == 0))
                    {
                        long count = tag.SynchronizationCount;
                        tag.SynchronizationCount = count + 1;

                        if (count >= 100)
                            count = 99;

                        tag.SynchronizationAvgTime = (tag.SynchronizationAvgTime * count + time) / (count + 1);
                    }

                    tag.ErrorCount = 0;
                }
                else
                {
                    // in caso di errore segnalo semplicemente la cosa
                    tag.ErrorCount++;
                    tag.LastErrorDateTime = _timeService.Now;

                    evt = TagScheduleEvent.BuildTagStatus(device, tag, err, description, errorStateChanged);
                }
            }

            return evt;
        }

        private static TagScheduleEvent ParseTagChangesAndMerge(Device device, Tag tag, bool errorStateChanged)
        {
            int start = int.MaxValue;
            int end = -1;

            for (int i = 0; i < tag.Size; i++)
            {
                if (tag.Data[i] != tag.OldData[i])
                {
                    if (i < start)
                        start = i;

                    end = i;

                    tag.OldData[i] = tag.Data[i];
                }
            }

            if (end >= 0)
            {
                return TagScheduleEvent.BuildTagData(device, tag, start + tag.ByteOffset, end - start + 1, errorStateChanged);
            }
            else
            {
                return TagScheduleEvent.BuildEmptyTagData(device, tag, errorStateChanged);
            }
        }

        private TagScheduleEvent WriteTag(TagSchedule schedule)
        {
            TagScheduleEvent evt;
            var device = schedule.Device;
            var tag = schedule.Tag;

            lock (tag)
            {
                // attenzione! la variabile bounds può essere null se il metodo non trova nessuna variazione
                // tra il datablock corrente e quello old.
                // Questo è causato dal fatto che il controllore è asincrono con le scritture del driver
                // quindi, se il controllore richidede una scrittura che poco dopo annulla con un'altra richiesta di scrittura,
                // se il driver non ha fatto in tempo a scrivere la prima richiesta sul device si ritrova ad avere il flag
                // "needsWrite" a true anche se le 2 richieste del controllore si sono annullate a vicenda.
                // Ad esempio: il controllore richiede di scrivere in DB22.DBX0.0 il valore 1 e invia il messaggio al driver.
                // Pochi istanti dopo, prima anche che il driver abbia scritto quel valore, il controllore richiede la scrittura
                // del valore 0 sempre in DB22.DBX0.0, annullando in pratica la richiesta precedente. Il flag "needsWrite" rimane a true
                // tuttavia non c'è alcun cambiamento sul datablock che deve effettivamente essere scritto.
                List<TagChangeBounds> listBounds = ParseWriteBounds(device, tag);
                if (!listBounds.Any())
                {
                    // se la lista dei bounds è vuota, interpreto la schedule come una scrittura completa
                    // questo è usato anche per gestire il parametro rewritePeriod dei tag in scrittura: ogni tot millisecondi, vengono comunque scritti tutti
                    // anche se non ci sono modifiche.
                    var chg = new TagChangeBounds
                    {
                        StartOffset = tag.ByteOffset,
                        Length = tag.Data.Length
                    };
                    listBounds.Add(chg);
                }

                int err = 0;
                string description = string.Empty;
                TimeSpan time;

                device.SetTagSynchronized(tag, _timeService.Now);
                var stopWatch = Stopwatch.StartNew();

                int totalStartOffset = int.MaxValue;
                int totalEndOffset = -1;

                foreach (TagChangeBounds bounds in listBounds)
                {
                    (err, description) = TryWithDeviceDriver(x => x.WriteTag(device, tag, tag.Data, bounds.StartOffset, bounds.Length));
                    if (err != 0)
                        break;

                    if (bounds.StartOffset < totalStartOffset)
                        totalStartOffset = bounds.StartOffset;
                    if (bounds.StartOffset + bounds.Length - 1 > totalEndOffset)
                        totalEndOffset = bounds.StartOffset + bounds.Length - 1;
                }
                time = stopWatch.Elapsed;

                int oldError = tag.ErrorCode;
                tag.ErrorCode = err;
                bool errorStateChanged = oldError != err;

                if (err == 0)
                {
                    // 2022-04-29 se la scrittura è andata a buon fine, copio in olddata i dati appena scritti; altrimenti, lo scheduler schedulerà di nuovo la scrittura
                    Array.Copy(tag.Data, 0, tag.OldData, 0, tag.Data.Length);
                    tag.IsWriteSynchronizationRequested = false;
                    tag.ErrorCount = 0;

                    if (time > TimeSpan.Zero)
                    {
                        long count = tag.SynchronizationCount;
                        tag.SynchronizationCount = count + 1;

                        if (count >= 100)
                            count = 99;

                        tag.SynchronizationAvgTime = (tag.SynchronizationAvgTime * count + time) / (count + 1);
                        tag.WritesCount += listBounds.Count;
                    }

                    evt = TagScheduleEvent.BuildTagData(device, tag, totalStartOffset, totalEndOffset - totalStartOffset + 1, errorStateChanged);
                }
                else
                {
                    tag.ErrorCount++;
                    tag.LastErrorDateTime = _timeService.Now;

                    evt = TagScheduleEvent.BuildTagStatus(device, tag, err, description, errorStateChanged);
                }
            }

            return evt;
        }

        private static List<TagChangeBounds> ParseWriteBounds(Device device, Tag tag)
        {
            var list = new List<TagChangeBounds>();
            TagChangeBounds? current = null;

            int singlePDUBytes = device.IsWriteOptimizationEnabled ? device.SinglePDUWriteBytes : 0;

            for (int i = 0; i < tag.Size; i++)
            {
                if (tag.Data[i] != tag.OldData[i])
                {
                    if (current == null || (singlePDUBytes > 0 && i - current.StartOffset + 1 > singlePDUBytes))
                    {
                        // nessun change corrente: lo creo
                        // oppure variazione fuori dalla pdu corrente: creo una nuova pdu
                        current = new TagChangeBounds
                        {
                            StartOffset = tag.ByteOffset + i,
                            Length = 1
                        };

                        list.Add(current);
                    }
                    else
                    {
                        // variazione all'interno della PDU corrente: aggiorno la pdu
                        // se singlePDUBytes <= 0, cado sempre qua dopo aver creato il primo change
                        current.Length = tag.ByteOffset + i + 1 - current.StartOffset;
                    }
                }
            }

            return list;
        }

        private void OnTagRead(TagScheduleEvent evt)
        {
            try
            {
                TagReadEvent?.Invoke(this, new TagScheduleEventArgs(DeviceDriver, evt));
            }
            catch (Exception ex)
            {
                try
                {
                    ExceptionHandler?.Invoke(this, new ExceptionEventArgs(ex));
                }
                catch
                {
                    // ignore this
                }
            }
        }

        private void OnTagWrite(TagScheduleEvent evt)
        {
            try
            {
                TagWriteEvent?.Invoke(this, new TagScheduleEventArgs(DeviceDriver, evt));
            }
            catch (Exception ex)
            {
                try
                {
                    ExceptionHandler?.Invoke(this, new ExceptionEventArgs(ex));
                }
                catch
                {
                    // ignore this
                }
            }
        }

        public bool IsRestartNeeded()
        {
            bool restart = _lastRestartInstant == null;
            if (!restart
                && _timeService.IsTimeoutElapsed(_lastRestartInstant!.Value, _configuration.RestartDeviceInErrorTimeout))
            {
                var device = DeviceDriver.Device;

                if (device.ErrorCount >= _configuration.MaxErrorsBeforeReconnection
                        || device.DeviceStatus == DeviceStatus.UNINITIALIZED
                        || device.DeviceStatus == DeviceStatus.ERROR)
                {
                    restart = true;
                }

                if (!restart)
                {
                    foreach (Tag tag in device.Tags)
                    {
                        if (tag.ErrorCount >= _configuration.MaxErrorsBeforeReconnection
                                || !tag.IsInitialized)
                        {
                            restart = true;
                            break;
                        }
                    }
                }
            }

            return restart;
        }

        public void RestartDriver()
        {
            if (!IsRestartNeeded())
                return;

            OnRestartingDeviceDriverEvent(new DeviceDriverRestartingEventArgs(DeviceDriver));
            bool success = true;
            string message = string.Empty;

            // devo far ripartire tutto: se non e' la prima partenza allora devo anche stoppare tutto.
            lock (DeviceDriver)
            {
                var device = DeviceDriver.Device;

                if (_lastRestartInstant != null)
                {
                    TryWithDeviceDriver(x => x.StopInterface());
                    TryWithDeviceDriver(x => x.Disconnect(device));
                }

                (int err, string description) = TryWithDeviceDriver(x => x.StartInterface());
                if (err != 0)
                {
                    success = false;
                    message = description;

                    device.IncrementOrReseDeviceErrorCode(err);

                    OnDeviceStatusEvent(new DeviceStatusEvent(device, device.DeviceStatus, err, description));
                }
                else
                {
                    (err, description) = TryWithDeviceDriver(x => x.Connect(device));
                    device.IncrementOrReseDeviceErrorCode(err);

                    if (err != 0)
                    {
                        success = false;
                        message = description;
                        OnDeviceStatusEvent(new DeviceStatusEvent(device, device.DeviceStatus, err, description));
                    }
                    else
                    {
                        OnDeviceStatusEvent(new DeviceStatusEvent(device, device.DeviceStatus, 0, string.Empty));

                        foreach (Tag tag in device.Tags)
                        {
                            if (tag.TagType == TagType.READ)
                            {
                                TagScheduleEvent? evt;

                                lock (tag)
                                {
                                    evt = ReadTag(new TagSchedule(device, tag, TagScheduleType.READ), false, true);
                                    if (evt == null)
                                        throw new InvalidOperationException("A complete read should always produce a TagRead event");

                                    if (evt.ErrorNumber == 0)
                                    {
                                        // se durante il restart del driver non ottengo nessun errore in lettura di un tag
                                        // lo segnalto interamente al gestore dell'evento.
                                        evt = TagScheduleEvent.BuildTagData(device, tag, true);
                                    }
                                    else
                                    {
                                        success = false;
                                        message = evt.Description!;
                                    }
                                }

                                OnTagRead(evt);
                            }
                            else /* TagType.WRITE */
                            {
                                // I tag in scrittura li leggo solo 1 volta, al momento della partenza o fino a quando non sono riuscito a leggerlo una volta
                                if (!tag.IsInitialized)
                                {
                                    TagScheduleEvent? evt;

                                    lock (tag)
                                    {
                                        evt = ReadTag(new TagSchedule(device, tag, TagScheduleType.READ), false, true);
                                        if (evt == null)
                                            throw new InvalidOperationException("A complete read should always produce a TagRead event");

                                        if (evt.ErrorNumber == 0)
                                        {
                                            evt = TagScheduleEvent.BuildTagData(device, tag, true);
                                        }
                                        else
                                        {
                                            success = false;
                                            message = evt.Description!;
                                        }
                                    }

                                    OnTagRead(evt);
                                }
                            }
                        }
                    }
                }

                _lastRestartInstant = _timeService.Now;
            }

            OnRestartedDeviceDriverEvent(new DeviceDriverRestartedEventArgs(DeviceDriver, success, message));
        }

        private void OnRestartingDeviceDriverEvent(DeviceDriverRestartingEventArgs args)
        {
            try
            {
                RestartingEvent?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                try
                {
                    ExceptionHandler?.Invoke(this, new ExceptionEventArgs(ex));
                }
                catch
                {
                    // ignore this
                }
            }
        }

        private void OnRestartedDeviceDriverEvent(DeviceDriverRestartedEventArgs args)
        {
            try
            {
                RestartedEvent?.Invoke(this, args);
            }
            catch (Exception ex)
            {
                try
                {
                    ExceptionHandler?.Invoke(this, new ExceptionEventArgs(ex));
                }
                catch
                {
                    // ignore this
                }
            }
        }

        private void OnDeviceStatusEvent(DeviceStatusEvent deviceStatusEvent)
        {
            try
            {
                DeviceStatusEvent?.Invoke(this, new DeviceStatusEventArgs(DeviceDriver, deviceStatusEvent));
            }
            catch (Exception ex)
            {
                try
                {
                    ExceptionHandler?.Invoke(this, new ExceptionEventArgs(ex));
                }
                catch
                {
                    // ignore this
                }
            }
        }

        private (int, string) TryWithDeviceDriver(Func<IDeviceDriver, int> action)
        {
            int err;
            string description = string.Empty;
            try
            {
                err = action.Invoke(DeviceDriver);
                if (err != 0)
                    description = DeviceDriver.GetErrorMessage(err);
            }
            catch (Exception ex)
            {
                err = -100;
                description = ex.GetFirstNonBlankMessageOrDefault() ?? ex.ToString();
            }

            return (err, description ?? "Unknown error");
        }
    }
}
