using SmartIOT.Connector.Core.Events;

namespace SmartIOT.Connector.Core.Connector;

public abstract class AbstractBufferedAggregatingConnector : AbstractConnector
{
    private readonly AggregatingConnectorEventQueue _eventQueue = new AggregatingConnectorEventQueue();
    private readonly CancellationTokenSource _stopToken = new CancellationTokenSource();
    public ISmartIOTConnectorInterface? ConnectorInterface { get; private set; }

    protected AbstractBufferedAggregatingConnector(string connectionString)
        : base(connectionString)
    {
    }

    protected abstract Task HandleOnTagReadEventAsync(object? sender, TagScheduleEventArgs args);

    protected abstract Task HandleOnTagWriteEventAsync(object? sender, TagScheduleEventArgs args);

    protected abstract Task HandleOnDeviceStatusEventAsync(object? sender, DeviceStatusEventArgs args);

    protected abstract Task HandleOnExceptionAsync(object? sender, ExceptionEventArgs args);

    /// <summary>
    /// Meaningful events are all data events that contains some data and tag-status events when the error changed.
    /// Applied when pushing onto the queue, so that events failing this test never enter it.
    /// </summary>
    private bool IsTagReadEventMeaningful(TagScheduleEventArgs tagScheduleEvent)
    {
        return (tagScheduleEvent.TagScheduleEvent.Data != null && tagScheduleEvent.TagScheduleEvent.Data.Length > 0)
            || tagScheduleEvent.TagScheduleEvent.IsErrorNumberChanged;
    }

    public override Task StartAsync(ISmartIOTConnectorInterface connectorInterface)
    {
        ConnectorInterface = connectorInterface;
        ConnectorInterface.OnConnectorStarted(new ConnectorStartedEventArgs(this, $"Connector started {ConnectionString}"));

        _ = Task.Factory.StartNew(async () =>
        {
            while (!_stopToken.IsCancellationRequested)
            {
                try
                {
                    var e = _eventQueue.PopWait(_stopToken.Token);
                    if (e != null)
                    {
                        if (e.ExceptionEvent.HasValue)
                            await HandleOnExceptionAsync(e.ExceptionEvent.Value.sender, e.ExceptionEvent.Value.args);
                        if (e.DeviceStatusEvent.HasValue)
                            await HandleOnDeviceStatusEventAsync(e.DeviceStatusEvent.Value.sender, e.DeviceStatusEvent.Value.args);
                        if (e.TagReadScheduleEvent.HasValue)
                            await HandleOnTagReadEventAsync(e.TagReadScheduleEvent.Value.sender, e.TagReadScheduleEvent.Value.args);
                        if (e.TagWriteScheduleEvent.HasValue)
                            await HandleOnTagWriteEventAsync(e.TagWriteScheduleEvent.Value.sender, e.TagWriteScheduleEvent.Value.args);
                    }
                }
                catch (OperationCanceledException)
                {
                    // cancellation event
                    break;
                }
                catch (Exception ex)
                {
                    try
                    {
                        ConnectorInterface!.OnConnectorException(new ConnectorExceptionEventArgs(this, ex));
                    }
                    catch
                    {
                        // ignore this
                    }
                }
            }
        }).Unwrap();

        return Task.CompletedTask;
    }

    public override Task StopAsync()
    {
        _stopToken.Cancel();
        _stopToken.Dispose();

        ConnectorInterface!.OnConnectorStopped(new ConnectorStoppedEventArgs(this, $"Connector stopped {ConnectionString}"));

        return Task.CompletedTask;
    }

    public override void OnException(object? sender, ExceptionEventArgs args)
    {
        _eventQueue.Push(CompositeConnectorEvent.Exception((sender, args)));
    }

    public override void OnDeviceStatusEvent(object? sender, DeviceStatusEventArgs args)
    {
        _eventQueue.Push(CompositeConnectorEvent.DeviceStatus((sender, args)));
    }

    public override void OnTagReadEvent(object? sender, TagScheduleEventArgs args)
    {
        // Filtered on the producer side: a read that changed nothing is the common case when the
        // device data is static, and it must not be enqueued only to be dropped by the consumer.
        if (IsTagReadEventMeaningful(args))
            _eventQueue.Push(CompositeConnectorEvent.TagRead((sender, args)));
    }

    public override void OnTagWriteEvent(object? sender, TagScheduleEventArgs args)
    {
        _eventQueue.Push(CompositeConnectorEvent.TagWrite((sender, args)));
    }
}
