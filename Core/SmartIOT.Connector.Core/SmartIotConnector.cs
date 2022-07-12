using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Events;
using SmartIOT.Connector.Core.Scheduler;
using SmartIOT.Connector.Core.Util;

namespace SmartIOT.Connector.Core
{
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

		public void Start()
		{
			Starting?.Invoke(this, new EventArgs());

			lock (_connectors)
			{
				foreach (var connector in _connectors)
				{
					connector.Start(this);
				}

				foreach (var scheduler in _schedulers)
				{
					scheduler.Start();
				}

				IsStarted = true;
			}

			Started?.Invoke(this, new EventArgs());
		}

		public void Stop()
		{
			Stopping?.Invoke(this, new EventArgs());

			foreach (var scheduler in _schedulers)
			{
				scheduler.Stop();
			}

			lock (_connectors)
			{
				foreach (var connector in _connectors)
				{
					connector.Stop();

					RemoveConnectorEvents(connector);
				}
			}

			Stopped?.Invoke(this, new EventArgs());
		}

		public int AddConnector(IConnector connector)
		{
			lock (_connectors)
			{
				_connectors.Add(connector);

				AddConnectorEvents(connector);

				if (IsStarted)
					connector.Start(this);

				return _connectors.IndexOf(connector);
			}
		}

		public bool RemoveConnector(int index)
		{
			lock (_connectors)
			{
				if (_connectors.Count <= index)
					return false;

				var connector = _connectors[index];
				_connectors.RemoveAt(index);

				if (IsStarted)
					connector.Stop();

				RemoveConnectorEvents(connector);

				return true;
			}
		}
		public bool RemoveConnector(IConnector connector)
		{
			lock (_connectors)
			{
				bool removed = _connectors.Remove(connector);

				if (IsStarted)
					connector.Stop();

				RemoveConnectorEvents(connector);

				return removed;
			}
		}

		public bool ReplaceConnector(int index, IConnector connector)
		{
			lock (_connectors)
			{
				if (!RemoveConnector(index))
					return false;
				
				_connectors.Insert(index, connector);

				AddConnectorEvents(connector);

				if (IsStarted)
				{
					connector.Start(this);
				}

				return true;
			}
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
			foreach (var scheduler in Schedulers)
			{
				var device = scheduler.Device;
				if (device.DeviceId.Equals(deviceId, StringComparison.InvariantCultureIgnoreCase))
				{
					var tags = device.Tags.Where(x => string.Equals(x.TagId, tagId, StringComparison.InvariantCultureIgnoreCase) && x.TagType == Conf.TagType.WRITE);

					foreach (var tag in tags)
					{
						lock (tag)
						{
							bool changes = MergeData(tag, startOffset, data);
							if (changes)
								tag.IsWriteSynchronizationRequested = true;
						}
					}
				}
			}
		}

		private bool MergeData(Model.Tag tag, int startOffset, byte[] data)
		{
			var somethingChanged = false;

			if (startOffset + data.Length > tag.ByteOffset && startOffset < tag.ByteOffset + tag.Size)
			{
				int start = Math.Max(startOffset, tag.ByteOffset);
				int end = Math.Min(startOffset + data.Length, tag.ByteOffset + tag.Data.Length);

				for (int i = start; i < end; i++)
				{
					byte newValue = data[i - startOffset];
					if (!somethingChanged)
					{
						byte oldValue = tag.Data[i - tag.ByteOffset];
						if (oldValue != newValue)
							somethingChanged = true;
					}
					tag.Data[i - tag.ByteOffset] = newValue;
				}
			}

			return somethingChanged;
		}

		public void RunInitializationAction(Action<IList<DeviceStatusEvent>, IList<TagScheduleEvent>> initAction)
		{
			foreach (var scheduler in _schedulers)
			{
				scheduler.RunInitializationAction(initAction);
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
}
