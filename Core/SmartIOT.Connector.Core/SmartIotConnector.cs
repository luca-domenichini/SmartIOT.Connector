using SmartIOT.Connector.Core.Events;
using SmartIOT.Connector.Core.Scheduler;

namespace SmartIOT.Connector.Core
{
	public class SmartIotConnector : ISmartIOTConnectorInterface
	{
		private readonly List<ITagScheduler> _schedulers;
		private readonly List<IConnector> _connectors;

		public IReadOnlyList<ITagScheduler> Schedulers => _schedulers;
		public IReadOnlyList<IConnector> Connectors => _connectors;

		public event EventHandler<EventArgs>? Starting;
		public event EventHandler<EventArgs>? Started;
		public event EventHandler<EventArgs>? Stopping;
		public event EventHandler<EventArgs>? Stopped;
		public event EventHandler<ConnectorConnectedEventArgs>? ConnectorConnected;
		public event EventHandler<ConnectorConnectionFailedEventArgs>? ConnectorConnectionFailed;
		public event EventHandler<ConnectorDisconnectedEventArgs>? ConnectorDisconnected;
		public event EventHandler<ConnectorExceptionEventArgs>? ConnectorException;

		public event EventHandler<DeviceDriverRestartingEventArgs>? SchedulerRestarting
		{
			add
			{
				foreach (var scheduler in Schedulers)
				{
					scheduler.EngineRestartingEvent += value;
				}
			}
			remove
			{
				foreach (var scheduler in Schedulers)
				{
					scheduler.EngineRestartingEvent -= value;
				}
			}
		}
		public event EventHandler<DeviceDriverRestartedEventArgs>? SchedulerRestarted
		{
			add
			{
				foreach (var scheduler in Schedulers)
				{
					scheduler.EngineRestartedEvent += value;
				}
			}
			remove
			{
				foreach (var scheduler in Schedulers)
				{
					scheduler.EngineRestartedEvent -= value;
				}
			}
		}
		public event EventHandler<TagSchedulerWaitExceptionEventArgs>? TagSchedulerWaitExceptionEvent
		{
			add
			{
				foreach (var scheduler in Schedulers)
				{
					scheduler.TagSchedulerWaitExceptionEvent += value;
				}
			}
			remove
			{
				foreach (var scheduler in Schedulers)
				{
					scheduler.TagSchedulerWaitExceptionEvent -= value;
				}
			}
		}
		public event EventHandler<TagScheduleEventArgs>? TagReadEvent
		{
			add
			{
				foreach (var scheduler in Schedulers)
				{
					scheduler.TagReadEvent += value;
				}
			}
			remove
			{
				foreach (var scheduler in Schedulers)
				{
					scheduler.TagReadEvent -= value;
				}
			}
		}
		public event EventHandler<TagScheduleEventArgs>? TagWriteEvent
		{
			add
			{
				foreach (var scheduler in Schedulers)
				{
					scheduler.TagWriteEvent += value;
				}
			}
			remove
			{
				foreach (var scheduler in Schedulers)
				{
					scheduler.TagWriteEvent -= value;
				}
			}
		}
		public event EventHandler<DeviceStatusEventArgs>? DeviceStatusEvent
		{
			add
			{
				foreach (var scheduler in Schedulers)
				{
					scheduler.DeviceStatusEvent += value;
				}
			}
			remove
			{
				foreach (var scheduler in Schedulers)
				{
					scheduler.DeviceStatusEvent -= value;
				}
			}
		}
		public event EventHandler<ExceptionEventArgs>? ExceptionHandler
		{
			add
			{
				foreach (var scheduler in Schedulers)
				{
					scheduler.ExceptionHandler += value;
				}
			}
			remove
			{
				foreach (var scheduler in Schedulers)
				{
					scheduler.ExceptionHandler -= value;
				}
			}
		}


		public SmartIotConnector(IList<ITagScheduler> schedulers)
			: this(schedulers, new List<IConnector>())
		{

		}
		public SmartIotConnector(IList<ITagScheduler> schedulers, IList<IConnector> connectors)
		{
			_schedulers = new List<ITagScheduler>(schedulers);
			_connectors = new List<IConnector>(connectors);
		}

		public void Start()
		{
			Starting?.Invoke(this, new EventArgs());

			foreach (var connector in _connectors)
			{
				connector.Start(this);
			}

			foreach (var scheduler in _schedulers)
			{
				scheduler.Start();
			}

			Started?.Invoke(this, new EventArgs());
		}

		public void RequestTagWrite(string deviceId, string tagId, int startOffset, byte[] data)
		{
			foreach (var scheduler in Schedulers)
			{
				var devices = scheduler.DeviceDriver.GetDevices(true);
				var tags = devices.Where(x => x.DeviceId == deviceId)
					.SelectMany(x => x.Tags)
					.Where(x => string.Equals(x.TagId, tagId, StringComparison.InvariantCultureIgnoreCase) && x.TagType == Conf.TagType.WRITE);

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

		public void Stop()
		{
			Stopping?.Invoke(this, new EventArgs());

			foreach (var scheduler in _schedulers)
			{
				scheduler.Stop();
			}

			foreach (var connector in _connectors)
			{
				connector.Stop();
			}

			Stopped?.Invoke(this, new EventArgs());
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
