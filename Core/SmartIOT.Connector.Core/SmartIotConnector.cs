using SmartIOT.Connector.Core.Events;
using SmartIOT.Connector.Core.Scheduler;

namespace SmartIOT.Connector.Core
{
	public class SmartIotConnector
	{
		private readonly List<ITagScheduler> _schedulers;
		private readonly List<IConnector> _connectors;
		private readonly ConnectorInterface _connectorInterface;

		public IReadOnlyList<ITagScheduler> Schedulers => _schedulers;
		public IReadOnlyList<IConnector> Connectors => _connectors;

		public event EventHandler<EventArgs>? Starting;
		public event EventHandler<EventArgs>? Started;
		public event EventHandler<EventArgs>? Stopping;
		public event EventHandler<EventArgs>? Stopped;
		public event EventHandler<ConnectorConnectedEventArgs>? ConnectorConnected;
		public event EventHandler<ConnectorDisconnectedEventArgs>? ConnectorDisconnected;

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
			_connectorInterface = new ConnectorInterface(
				RunInitializationAction
				, RequestDataWrite
				, (connector, info) => ConnectorConnected?.Invoke(this, new ConnectorConnectedEventArgs(new ConnectorConnectedEvent(connector, info)))
				, (connector, info) => ConnectorDisconnected?.Invoke(this, new ConnectorDisconnectedEventArgs(new ConnectorDisconnectedEvent(connector, info)))
			);

			_schedulers = new List<ITagScheduler>(schedulers);
			_connectors = new List<IConnector>(connectors);
		}

		public void Start()
		{
			Starting?.Invoke(this, new EventArgs());

			foreach (var connector in _connectors)
			{
				connector.Start(_connectorInterface);
			}

			foreach (var scheduler in _schedulers)
			{
				scheduler.Start();
			}

			Started?.Invoke(this, new EventArgs());
		}

		private void RequestDataWrite(string deviceId, string tagId, int startOffset, byte[] data)
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

		private void RunInitializationAction(Action<IList<DeviceStatusEvent>, IList<TagScheduleEvent>> initAction, Action afterInitAction)
		{
			foreach (var scheduler in _schedulers)
			{
				scheduler.RunInitializationAction(initAction);
			}

			afterInitAction.Invoke();
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

	}
}
