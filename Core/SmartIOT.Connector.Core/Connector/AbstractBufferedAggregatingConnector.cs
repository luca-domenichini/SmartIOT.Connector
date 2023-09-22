using SmartIOT.Connector.Core.Events;

namespace SmartIOT.Connector.Core.Connector
{
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
        /// Meaningful events are all data events that contains some data and tag-status events when the error changed
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
                            if (e.TagReadScheduleEvent.HasValue && IsTagReadEventMeaningful(e.TagReadScheduleEvent.Value.args))
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
            _eventQueue.Push(CompositeConnectorEvent.TagRead((sender, args)));
        }

        public override void OnTagWriteEvent(object? sender, TagScheduleEventArgs args)
        {
            _eventQueue.Push(CompositeConnectorEvent.TagWrite((sender, args)));
        }
    }
}
