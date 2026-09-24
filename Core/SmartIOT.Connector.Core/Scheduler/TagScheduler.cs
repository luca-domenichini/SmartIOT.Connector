using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Events;
using SmartIOT.Connector.Core.Model;
using System.Collections.Concurrent;

namespace SmartIOT.Connector.Core.Scheduler;

public class TagScheduler : ITagScheduler
{
    private readonly ITagSchedulerEngine _tagSchedulerEngine;
    private readonly ITimeService _timeService;
    private readonly SchedulerConfiguration _configuration;
    private readonly CancellationTokenSource _terminatingToken = new CancellationTokenSource();
    private readonly Thread _schedulerThread;
    private readonly SemaphoreSlim _schedulerTerminated = new SemaphoreSlim(0, 1);
    private readonly Thread _monitorThread;
    private readonly SemaphoreSlim _monitorThreadTerminated = new SemaphoreSlim(0, 1);

    /// <summary>
    /// Set while the scheduler is allowed to run, cleared while it is paused. It is also set when a
    /// stop is requested, so that a paused scheduler wakes up and can observe the termination
    /// request instead of sleeping through it.
    /// </summary>
    private readonly ManualResetEventSlim _runningSignal = new ManualResetEventSlim(true);

    private long _terminatingInstant;   // written by StopAsync, read by the scheduler thread
    private long _lastWriteOnDevice;    // read/written by the scheduler thread
    private readonly object _pauseLock = new();
    private bool _isPaused;
    private readonly ConcurrentDictionary<Device, DeviceStatusEvent> _lastDeviceStatusEvents = new ConcurrentDictionary<Device, DeviceStatusEvent>();

    public event EventHandler<SchedulerStartingEventArgs>? SchedulerStarting;

    public event EventHandler<SchedulerStoppingEventArgs>? SchedulerStopping;

    public event EventHandler<TagSchedulerWaitingEventArgs>? TagSchedulerWaitingEvent;

    public event EventHandler<DeviceDriverRestartingEventArgs>? EngineRestartingEvent;

    public event EventHandler<DeviceDriverRestartedEventArgs>? EngineRestartedEvent;

    public event EventHandler<TagScheduleEventArgs>? TagReadEvent;

    public event EventHandler<TagScheduleEventArgs>? TagWriteEvent;

    public event EventHandler<DeviceStatusEventArgs>? DeviceStatusEvent;

    public event EventHandler<ExceptionEventArgs>? ExceptionHandler;

    public bool IsPaused
    {
        get
        {
            lock (_pauseLock)
                return _isPaused;
        }
        set
        {
            lock (_pauseLock)
            {
                _isPaused = value;
                if (value)
                    _runningSignal.Reset();
                else
                    _runningSignal.Set();
            }
        }
    }

    public IDeviceDriver DeviceDriver => _tagSchedulerEngine.DeviceDriver;
    public Device Device => DeviceDriver.Device;

    public TagScheduler(string name, ITagSchedulerEngine tagSchedulerEngine, ITimeService timeService, SchedulerConfiguration configuration)
    {
        _tagSchedulerEngine = tagSchedulerEngine;
        _tagSchedulerEngine.RestartingEvent += OnEngineRestartingHandler;
        _tagSchedulerEngine.RestartedEvent += OnEngineRestartedHandler;
        _tagSchedulerEngine.ExceptionHandler += OnEngineExceptionHandler;
        _tagSchedulerEngine.DeviceStatusEvent += OnEngineDeviceStatusEvent;
        _tagSchedulerEngine.TagReadEvent += OnEngineTagReadEvent;
        _tagSchedulerEngine.TagWriteEvent += OnEngineTagWriteEvent;

        _timeService = timeService;
        _configuration = configuration;

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
        try
        {
            while (true)
            {
                if (!_terminatingToken.IsCancellationRequested && IsPaused)
                {
                    try
                    {
                        // blocks until IsPaused goes back to false, instead of waking up once a
                        // second to poll for a change that never comes
                        _runningSignal.Wait(_terminatingToken.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // we must verify if stopping application is possible or if there is something more to write to tags
                    }

                    continue;
                }

                if (_terminatingToken.IsCancellationRequested)
                {
                    // long is not a volatile-able type in C#, so the cross-thread hand-off is made
                    // explicit: reading 0 here would make the drain timeouts look already elapsed
                    long terminatingInstant = Volatile.Read(ref _terminatingInstant);
                    long lastWriteOnDevice = Volatile.Read(ref _lastWriteOnDevice);

                    var lastWriteOrTerminateInstant = terminatingInstant < lastWriteOnDevice ? lastWriteOnDevice : terminatingInstant;
                    var now = _timeService.NowTicks;

                    if (_timeService.IsTimeoutElapsed(lastWriteOrTerminateInstant, now, _configuration.TerminateAfterNoWriteRequestsDelay)
                        && _timeService.IsTimeoutElapsed(terminatingInstant, now, _configuration.TerminateMinimumDelay))
                        break;
                }

                try
                {
                    if (_tagSchedulerEngine.TryScheduleNextTag(_terminatingToken.IsCancellationRequested, out var schedule, out var waitTime))
                    {
                        if (schedule.Type == TagScheduleType.WRITE)
                            Volatile.Write(ref _lastWriteOnDevice, _timeService.NowTicks);
                    }
                    else
                    {
                        NotifySchedulerWaiting(waitTime);

                        if (_terminatingToken.IsCancellationRequested)
                        {
                            // already stopping: sleeping without waiting on the termination token,
                            // otherwise WaitOne would return immediately and the loop would spin
                            Thread.Sleep(waitTime);
                        }
                        else
                        {
                            // sleep exactly the wait time the engine asked for, waking up early if
                            // a stop request arrives in the meantime
                            _terminatingToken.Token.WaitHandle.WaitOne(waitTime);
                        }
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
        finally
        {
            _schedulerTerminated.Release();
        }
    }

    /// <summary>
    /// Raises the waiting event only when subscribed, avoiding idle-path event-args allocations.
    /// </summary>
    private void NotifySchedulerWaiting(TimeSpan waitTime)
    {
        if (TagSchedulerWaitingEvent is { } handler)
            handler.Invoke(this, new TagSchedulerWaitingEventArgs(waitTime));
    }

    private void MonitorThreadRun()
    {
        try
        {
            while (true)
            {
                try
                {
                    _tagSchedulerEngine.RestartDriver();

                    if (!_terminatingToken.IsCancellationRequested)
                        _terminatingToken.Token.WaitHandle.WaitOne(500);
                    else
                        break;
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
        finally
        {
            _monitorThreadTerminated.Release();
        }
    }

    public Task StartAsync()
    {
        SchedulerStarting?.Invoke(this, new SchedulerStartingEventArgs(this));

        _schedulerThread.Start();
        _monitorThread.Start();

        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        SchedulerStopping?.Invoke(this, new SchedulerStoppingEventArgs(this));

        // the stop instant is published before the cancellation, so the scheduler thread is
        // guaranteed to read a valid value as soon as it observes the cancellation
        Volatile.Write(ref _terminatingInstant, _timeService.NowTicks);

        // wake up a paused scheduler so it can observe the stop request too
        _runningSignal.Set();

        _terminatingToken.Cancel();

        await _schedulerTerminated.WaitAsync();
        await _monitorThreadTerminated.WaitAsync();

        _terminatingToken.Dispose();

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
