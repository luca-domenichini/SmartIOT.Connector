using SmartIOT.Connector.Core.Events;
using SmartIOT.Connector.Core.Model;
using System.Collections.Concurrent;

namespace SmartIOT.Connector.Core.Scheduler
{
    public class TagScheduler : ITagScheduler
    {
        private readonly ITagSchedulerEngine _tagSchedulerEngine;
        private readonly ITimeService _timeService;
        private readonly static TimeSpan TerminateAfterNoWriteRequestsDelay = TimeSpan.FromSeconds(3);
        private readonly static TimeSpan TerminateMinimumDelay = TimeSpan.FromSeconds(3);
        private readonly CancellationTokenSource _terminatingToken = new CancellationTokenSource();
        private readonly Thread _schedulerThread;
        private readonly Thread _monitorThread;
        private DateTime _terminatingInstant;
        private DateTime _lastWriteOnDevice;
        private readonly ConcurrentDictionary<Device, DeviceStatusEvent> _lastDeviceStatusEvents = new ConcurrentDictionary<Device, DeviceStatusEvent>();

        public event EventHandler<SchedulerStartingEventArgs>? SchedulerStarting;

        public event EventHandler<SchedulerStoppingEventArgs>? SchedulerStopping;

        public event EventHandler<TagSchedulerWaitExceptionEventArgs>? TagSchedulerWaitExceptionEvent;

        public event EventHandler<DeviceDriverRestartingEventArgs>? EngineRestartingEvent;

        public event EventHandler<DeviceDriverRestartedEventArgs>? EngineRestartedEvent;

        public event EventHandler<TagScheduleEventArgs>? TagReadEvent;

        public event EventHandler<TagScheduleEventArgs>? TagWriteEvent;

        public event EventHandler<DeviceStatusEventArgs>? DeviceStatusEvent;

        public event EventHandler<ExceptionEventArgs>? ExceptionHandler;

        public bool IsPaused { get; set; }
        public IDeviceDriver DeviceDriver => _tagSchedulerEngine.DeviceDriver;
        public Device Device => DeviceDriver.Device;

        public TagScheduler(string name, ITagSchedulerEngine tagSchedulerEngine, ITimeService timeService)
        {
            _tagSchedulerEngine = tagSchedulerEngine;
            _tagSchedulerEngine.RestartingEvent += OnEngineRestartingHandler;
            _tagSchedulerEngine.RestartedEvent += OnEngineRestartedHandler;
            _tagSchedulerEngine.ExceptionHandler += OnEngineExceptionHandler;
            _tagSchedulerEngine.DeviceStatusEvent += OnEngineDeviceStatusEvent;
            _tagSchedulerEngine.TagReadEvent += OnEngineTagReadEvent;
            _tagSchedulerEngine.TagWriteEvent += OnEngineTagWriteEvent;

            _timeService = timeService;

            _schedulerThread = new Thread(SchedulerThreadRun)
            {
                Name = $"TagSchedulerThread.{name}"
            };
            _monitorThread = new Thread(MonitorThreadRun)
            {
                Name = $"MonitorThread.{name}"
            };
        }

        private void OnEngineTagWriteEvent(object? sender, TagScheduleEventArgs e)
        {
            TagWriteEvent?.Invoke(this, e);
        }

        private void OnEngineTagReadEvent(object? sender, TagScheduleEventArgs e)
        {
            TagReadEvent?.Invoke(this, e);
        }

        private void OnEngineDeviceStatusEvent(object? sender, DeviceStatusEventArgs e)
        {
            _lastDeviceStatusEvents[e.DeviceStatusEvent.Device] = e.DeviceStatusEvent;

            DeviceStatusEvent?.Invoke(this, e);
        }

        private void OnEngineRestartingHandler(object? sender, DeviceDriverRestartingEventArgs e)
        {
            EngineRestartingEvent?.Invoke(this, e);
        }

        private void OnEngineRestartedHandler(object? sender, DeviceDriverRestartedEventArgs e)
        {
            EngineRestartedEvent?.Invoke(this, e);
        }

        private void OnEngineExceptionHandler(object? sender, ExceptionEventArgs e)
        {
            ExceptionHandler?.Invoke(this, e);
        }

        private void SchedulerThreadRun()
        {
            while (true)
            {
                if (!_terminatingToken.IsCancellationRequested && IsPaused)
                {
                    try
                    {
                        _terminatingToken.Token.WaitHandle.WaitOne(1000);
                    }
                    catch (OperationCanceledException)
                    {
                        // we must verify if stopping application is possible or if there is something more to write to tags
                    }

                    continue;
                }

                if (_terminatingToken.IsCancellationRequested)
                {
                    var istanteUltimaAzione = _terminatingInstant < _lastWriteOnDevice ? _lastWriteOnDevice : _terminatingInstant;
                    var now = _timeService.Now;

                    if (_timeService.IsTimeoutElapsed(istanteUltimaAzione, now, TerminateAfterNoWriteRequestsDelay)
                        && _timeService.IsTimeoutElapsed(_terminatingInstant, now, TerminateMinimumDelay))
                        break;
                }

                try
                {
                    try
                    {
                        var schedule = _tagSchedulerEngine.ScheduleNextTag(_terminatingToken.IsCancellationRequested);
                        if (schedule != null)
                        {
                            if (schedule.Type == TagScheduleType.WRITE)
                                _lastWriteOnDevice = _timeService.Now;
                        }
                        else
                        {
                            Thread.Sleep(1000);
                        }
                    }
                    catch (TagSchedulerWaitException ex)
                    {
                        TagSchedulerWaitExceptionEvent?.Invoke(this, new TagSchedulerWaitExceptionEventArgs(ex));

                        Thread.Sleep(ex.WaitTime);
                    }
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
        }

        private void MonitorThreadRun()
        {
            while (true)
            {
                try
                {
                    if (!_terminatingToken.IsCancellationRequested)
                        _terminatingToken.Token.WaitHandle.WaitOne(500);
                    else
                        break;

                    _tagSchedulerEngine.RestartDriver();
                }
                catch (OperationCanceledException)
                {
                    // stop request: exit
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
        }

        public void Start()
        {
            SchedulerStarting?.Invoke(this, new SchedulerStartingEventArgs(this));

            _schedulerThread.Start();
            _monitorThread.Start();
        }

        public void Stop()
        {
            SchedulerStopping?.Invoke(this, new SchedulerStoppingEventArgs(this));

            _terminatingToken.Cancel();
            _terminatingInstant = _timeService.Now;

            _schedulerThread.Join();
            _monitorThread.Join();

            _tagSchedulerEngine.ExceptionHandler -= OnEngineExceptionHandler;
            _tagSchedulerEngine.DeviceStatusEvent -= OnEngineDeviceStatusEvent;
            _tagSchedulerEngine.TagReadEvent -= OnEngineTagReadEvent;
            _tagSchedulerEngine.TagWriteEvent -= OnEngineTagWriteEvent;
        }

        public async Task RunInitializationActionAsync(Func<IList<DeviceStatusEvent>, IList<TagScheduleEvent>, Task> initializationAction)
        {
            List<DeviceStatusEvent> statuses;
            List<TagScheduleEvent> tags;

            lock (_tagSchedulerEngine.DeviceDriver)
            {
                statuses = new List<DeviceStatusEvent>(_lastDeviceStatusEvents.Values);
                tags = Device.Tags.Select(x => TagScheduleEvent.BuildTagData(Device, x, true)).ToList();
            }

            await initializationAction.Invoke(statuses, tags);
        }
    }
}
