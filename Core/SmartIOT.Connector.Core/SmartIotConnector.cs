using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Events;
using SmartIOT.Connector.Core.Scheduler;
using SmartIOT.Connector.Core.Util;

namespace SmartIOT.Connector.Core;

public class SmartIotConnector : ISmartIOTConnectorInterface
{
    private readonly CopyOnWriteArrayList<ITagScheduler> _schedulers = new CopyOnWriteArrayList<ITagScheduler>();
    private readonly CopyOnWriteArrayList<IConnector> _connectors = new CopyOnWriteArrayList<IConnector>();

    public IReadOnlyList<ITagScheduler> Schedulers => _schedulers;
    public IReadOnlyList<IConnector> Connectors => _connectors;
    public SchedulerConfiguration SchedulerConfiguration { get; } = new SchedulerConfiguration();
    public bool IsStarted { get; private set; }

    public event EventHandler<EventArgs>? Starting;

    public event EventHandler<EventArgs>? Started;

    public event EventHandler<EventArgs>? Stopping;

    public event EventHandler<EventArgs>? Stopped;

    public event EventHandler<ConnectorStartedEventArgs>? ConnectorStarted;

    public event EventHandler<ConnectorStoppedEventArgs>? ConnectorStopped;

    public event EventHandler<ConnectorConnectedEventArgs>? ConnectorConnected;

    public event EventHandler<ConnectorConnectionFailedEventArgs>? ConnectorConnectionFailed;

    public event EventHandler<ConnectorDisconnectedEventArgs>? ConnectorDisconnected;

    public event EventHandler<ConnectorExceptionEventArgs>? ConnectorException;

    public event EventHandler<SchedulerStartingEventArgs>? SchedulerStarting;

    public event EventHandler<SchedulerStoppingEventArgs>? SchedulerStopping;

    public event EventHandler<DeviceDriverRestartingEventArgs>? SchedulerRestarting;

    public event EventHandler<DeviceDriverRestartedEventArgs>? SchedulerRestarted;

    public event EventHandler<TagSchedulerWaitExceptionEventArgs>? TagSchedulerWaitExceptionEvent;

    public event EventHandler<TagScheduleEventArgs>? TagReadEvent;

    public event EventHandler<TagScheduleEventArgs>? TagWriteEvent;

    public event EventHandler<DeviceStatusEventArgs>? DeviceStatusEvent;

    public event EventHandler<ExceptionEventArgs>? ExceptionHandler;

    public SmartIotConnector()
    {
    }

    public SmartIotConnector(IList<ITagScheduler> schedulers, IList<IConnector> connectors, SchedulerConfiguration schedulerConfiguration)
    {
        SchedulerConfiguration = schedulerConfiguration;

        foreach (var connector in connectors)
        {
            AddConnector(connector);
        }

        foreach (var scheduler in schedulers)
        {
            AddScheduler(scheduler);
        }
    }

    public async Task StartAsync()
    {
        Starting?.Invoke(this, new EventArgs());

        foreach (var connector in _connectors)
        {
            await connector.StartAsync(this);
        }

        foreach (var scheduler in _schedulers)
        {
            scheduler.Start();
        }

        IsStarted = true;

        Started?.Invoke(this, new EventArgs());
    }

    public async Task StopAsync()
    {
        Stopping?.Invoke(this, new EventArgs());

        foreach (var scheduler in _schedulers)
        {
            scheduler.Stop();
        }

        foreach (var connector in _connectors)
        {
            await connector.StopAsync();

            RemoveConnectorEvents(connector);
        }

        Stopped?.Invoke(this, new EventArgs());
    }

    private void AddConnector(IConnector connector)
    {
        AddConnectorEvents(connector);

        _connectors.Add(connector);
    }

    public async Task<int> AddConnectorAsync(IConnector connector)
    {
        AddConnectorEvents(connector);

        if (IsStarted)
            await connector.StartAsync(this);

        return _connectors.Add(connector);
    }

    public async Task<bool> RemoveConnectorAsync(int index)
    {
        if (!_connectors.TryRemoveAt(index, out var connector))
            return false;

        if (IsStarted)
            await connector!.StopAsync();

        RemoveConnectorEvents(connector!);

        return true;
    }

    public async Task<bool> RemoveConnectorAsync(IConnector connector)
    {
        bool removed = _connectors.Remove(connector);

        if (IsStarted)
            await connector.StopAsync();

        RemoveConnectorEvents(connector);

        return removed;
    }

    public async Task<bool> ReplaceConnectorAsync(int index, IConnector connector)
    {
        if (!_connectors.TryReplaceAt(index, connector, out var oldConnector))
            return false;

        AddConnectorEvents(connector);

        if (IsStarted)
        {
            await oldConnector!.StopAsync();
            await connector.StartAsync(this);
        }

        return true;
    }

    protected void AddConnectorEvents(IConnector connector)
    {
        TagReadEvent += connector.OnTagReadEvent;
        TagWriteEvent += connector.OnTagWriteEvent;
        DeviceStatusEvent += connector.OnDeviceStatusEvent;
        ExceptionHandler += connector.OnException;
    }

    protected void RemoveConnectorEvents(IConnector connector)
    {
        TagReadEvent -= connector.OnTagReadEvent;
        TagWriteEvent -= connector.OnTagWriteEvent;
        DeviceStatusEvent -= connector.OnDeviceStatusEvent;
        ExceptionHandler -= connector.OnException;
    }

    public void AddScheduler(ITagScheduler scheduler)
    {
        _schedulers.Add(scheduler);

        scheduler.TagReadEvent += OnSchedulerTagReadEvent;
        scheduler.TagWriteEvent += OnSchedulerTagWriteEvent;
        scheduler.DeviceStatusEvent += OnSchedulerDeviceStatusEvent;
        scheduler.ExceptionHandler += OnSchedulerException;

        scheduler.EngineRestartingEvent += OnSchedulerRestartingEvent;
        scheduler.EngineRestartedEvent += OnSchedulerRestartedEvent;
        scheduler.TagSchedulerWaitExceptionEvent += OnSchedulerWaitExceptionEvent;

        scheduler.SchedulerStarting += OnSchedulerStarting;
        scheduler.SchedulerStopping += OnSchedulerStopping;

        if (IsStarted)
            scheduler.Start();
    }

    public void RemoveScheduler(ITagScheduler scheduler)
    {
        _schedulers.Remove(scheduler);

        if (IsStarted)
            scheduler.Stop();

        scheduler.TagReadEvent -= OnSchedulerTagReadEvent;
        scheduler.TagWriteEvent -= OnSchedulerTagWriteEvent;
        scheduler.DeviceStatusEvent -= OnSchedulerDeviceStatusEvent;
        scheduler.ExceptionHandler -= OnSchedulerException;

        scheduler.EngineRestartingEvent += OnSchedulerRestartingEvent;
        scheduler.EngineRestartedEvent += OnSchedulerRestartedEvent;
        scheduler.TagSchedulerWaitExceptionEvent += OnSchedulerWaitExceptionEvent;
    }

    private void OnSchedulerStopping(object? sender, SchedulerStoppingEventArgs e)
    {
        SchedulerStopping?.Invoke(this, e);
    }

    private void OnSchedulerStarting(object? sender, SchedulerStartingEventArgs e)
    {
        SchedulerStarting?.Invoke(this, e);
    }

    private void OnSchedulerRestartingEvent(object? sender, DeviceDriverRestartingEventArgs e)
    {
        SchedulerRestarting?.Invoke(this, e);
    }

    private void OnSchedulerRestartedEvent(object? sender, DeviceDriverRestartedEventArgs e)
    {
        SchedulerRestarted?.Invoke(this, e);
    }

    private void OnSchedulerWaitExceptionEvent(object? sender, TagSchedulerWaitExceptionEventArgs e)
    {
        TagSchedulerWaitExceptionEvent?.Invoke(this, e);
    }

    private void OnSchedulerTagReadEvent(object? sender, TagScheduleEventArgs e)
    {
        TagReadEvent?.Invoke(this, e);
    }

    private void OnSchedulerTagWriteEvent(object? sender, TagScheduleEventArgs e)
    {
        TagWriteEvent?.Invoke(this, e);
    }

    private void OnSchedulerDeviceStatusEvent(object? sender, DeviceStatusEventArgs e)
    {
        DeviceStatusEvent?.Invoke(this, e);
    }

    private void OnSchedulerException(object? sender, ExceptionEventArgs e)
    {
        ExceptionHandler?.Invoke(this, e);
    }

    public void RequestTagWrite(string deviceId, string tagId, int startOffset, byte[] data)
    {
        foreach (var tag in Schedulers.Select(x => x.Device)
            .Where(device => device.DeviceId.Equals(deviceId, StringComparison.InvariantCultureIgnoreCase))
            .SelectMany(device => device.Tags)
            .Where(tag => string.Equals(tag.TagId, tagId, StringComparison.InvariantCultureIgnoreCase) && tag.TagType == TagType.WRITE)
            )
        {
            tag.TryMergeData(data, startOffset, data.Length);
        }
    }

    public async Task RunInitializationActionAsync(Func<IList<DeviceStatusEvent>, IList<TagScheduleEvent>, Task> initAction)
    {
        foreach (var scheduler in _schedulers)
        {
            await scheduler.RunInitializationActionAsync(initAction);
        }
    }

    public void OnConnectorStarted(ConnectorStartedEventArgs args)
    {
        ConnectorStarted?.Invoke(this, args);
    }

    public void OnConnectorStopped(ConnectorStoppedEventArgs args)
    {
        ConnectorStopped?.Invoke(this, args);
    }

    public void OnConnectorConnected(ConnectorConnectedEventArgs args)
    {
        ConnectorConnected?.Invoke(this, args);
    }

    public void OnConnectorConnectionFailed(ConnectorConnectionFailedEventArgs args)
    {
        ConnectorConnectionFailed?.Invoke(this, args);
    }

    public void OnConnectorDisconnected(ConnectorDisconnectedEventArgs args)
    {
        ConnectorDisconnected?.Invoke(this, args);
    }

    public void OnConnectorException(ConnectorExceptionEventArgs args)
    {
        ConnectorException?.Invoke(this, args);
    }
}
