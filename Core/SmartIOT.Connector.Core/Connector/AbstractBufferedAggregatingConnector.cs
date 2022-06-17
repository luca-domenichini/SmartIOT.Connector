using SmartIOT.Connector.Core.Events;

namespace SmartIOT.Connector.Core.Connector
{
	public abstract class AbstractBufferedAggregatingConnector : IConnector
	{
		private readonly AggregatingConnectorEventQueue _eventQueue = new AggregatingConnectorEventQueue();
		private readonly Thread? _thread;
		private readonly CancellationTokenSource _stopToken = new CancellationTokenSource();
		public ISmartIOTConnectorInterface? ConnectorInterface { get; private set; }

		public AbstractBufferedAggregatingConnector()
		{
			_thread = new Thread(RunInnerThread)
			{
				Name = GetType().Name,
			};
		}

		protected abstract void HandleOnTagReadEvent(object? sender, TagScheduleEventArgs args);
		protected abstract void HandleOnTagWriteEvent(object? sender, TagScheduleEventArgs args);
		protected abstract void HandleOnDeviceStatusEvent(object? sender, DeviceStatusEventArgs args);
		protected abstract void HandleOnException(object? sender, ExceptionEventArgs args);


		private void RunInnerThread(object? obj)
		{
			while (!_stopToken.IsCancellationRequested)
			{
				try
				{
					var e = _eventQueue.PopWait(_stopToken.Token);
					if (e != null)
					{
						if (e.ExceptionEvent.HasValue)
							HandleOnException(e.ExceptionEvent.Value.sender, e.ExceptionEvent.Value.args);
						if (e.DeviceStatusEvent.HasValue)
							HandleOnDeviceStatusEvent(e.DeviceStatusEvent.Value.sender, e.DeviceStatusEvent.Value.args);
						if (e.TagReadScheduleEvent.HasValue && IsTagReadEventMeaningful(e.TagReadScheduleEvent.Value.args))
							HandleOnTagReadEvent(e.TagReadScheduleEvent.Value.sender, e.TagReadScheduleEvent.Value.args);
						if (e.TagWriteScheduleEvent.HasValue)
							HandleOnTagWriteEvent(e.TagWriteScheduleEvent.Value.sender, e.TagWriteScheduleEvent.Value.args);
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
		}

		/// <summary>
		/// Meaningful events are all data events that contains some data and tag-status events when the error changed
		/// </summary>
		private bool IsTagReadEventMeaningful(TagScheduleEventArgs tagScheduleEvent)
		{
			return tagScheduleEvent.TagScheduleEvent.Data != null && tagScheduleEvent.TagScheduleEvent.Data.Length > 0
				|| tagScheduleEvent.TagScheduleEvent.IsErrorNumberChanged;
		}

		public virtual void Start(ISmartIOTConnectorInterface connectorInterface)
		{
			ConnectorInterface = connectorInterface;
			ConnectorInterface.OnConnectorStarted(new ConnectorStartedEventArgs(this, $"Connector started"));

			_thread!.Start();
		}

		public virtual void Stop()
		{
			_stopToken.Cancel();

			try
			{
				_thread!.Join();
			}
			catch (Exception ex) when (ex is ThreadInterruptedException || ex is ThreadStateException)
			{
				// ignore
			}

			ConnectorInterface!.OnConnectorStopped(new ConnectorStoppedEventArgs(this, $"Connector stopped"));
		}


		public void OnException(object? sender, ExceptionEventArgs args)
		{
			_eventQueue.Push(CompositeConnectorEvent.Exception((sender, args)));
		}

		public void OnDeviceStatusEvent(object? sender, DeviceStatusEventArgs args)
		{
			_eventQueue.Push(CompositeConnectorEvent.DeviceStatus((sender, args)));
		}

		public void OnTagReadEvent(object? sender, TagScheduleEventArgs args)
		{
			_eventQueue.Push(CompositeConnectorEvent.TagRead((sender, args)));
		}

		public void OnTagWriteEvent(object? sender, TagScheduleEventArgs args)
		{
			_eventQueue.Push(CompositeConnectorEvent.TagWrite((sender, args)));
		}
	}
}
