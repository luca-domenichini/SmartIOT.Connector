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
		private bool _terminating;
		private readonly Thread _schedulerThread;
		private readonly Thread _monitorThread;
		private DateTime _terminatingInstant = new DateTime(0);
		private DateTime _lastWriteOnDevice = new DateTime(0);
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
				if (!_terminating)
				{
					if (IsPaused)
					{
						Thread.Sleep(1000);
						continue;
					}
				}

				lock (this)
				{
					if (_terminating)
					{
						var istanteUltimaAzione = _terminatingInstant < _lastWriteOnDevice ? _lastWriteOnDevice : _terminatingInstant;
						var now = _timeService.Now;
						
						if (_timeService.IsTimeoutElapsed(istanteUltimaAzione, now, TerminateAfterNoWriteRequestsDelay)
							&& _timeService.IsTimeoutElapsed(_terminatingInstant, now, TerminateMinimumDelay))
							break;
					}
				}

				try
				{
					try
					{
						var schedule = _tagSchedulerEngine.ScheduleNextTag(_terminating);
						if (schedule != null)
						{
							if (schedule.Type == TagScheduleType.WRITE)
								_lastWriteOnDevice = _timeService.Now;
						}
						else
						{
							Monitor.Wait(this, 1000);
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
					lock (this)
					{
						if (!_terminating)
							Monitor.Wait(this, 500);

						if (_terminating)
							break;
					}

					_tagSchedulerEngine.RestartDriver();
				}
				catch (Exception ex)
				{
					try
					{
						ExceptionHandler?.Invoke(this, new ExceptionEventArgs(ex));
					}
					catch
					{
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

			lock (this)
			{
				_terminating = true;
				_terminatingInstant = _timeService.Now;
			}

			_schedulerThread.Join();
			_monitorThread.Join();

			_tagSchedulerEngine.ExceptionHandler -= OnEngineExceptionHandler;
			_tagSchedulerEngine.DeviceStatusEvent -= OnEngineDeviceStatusEvent;
			_tagSchedulerEngine.TagReadEvent -= OnEngineTagReadEvent;
			_tagSchedulerEngine.TagWriteEvent -= OnEngineTagWriteEvent;
		}

		public void RunInitializationAction(Action<IList<DeviceStatusEvent>, IList<TagScheduleEvent>> initializationAction)
		{
			lock (_tagSchedulerEngine.DeviceDriver)
			{
				initializationAction.Invoke(new List<DeviceStatusEvent>(_lastDeviceStatusEvents.Values), Device.Tags.Select(x => TagScheduleEvent.BuildTagData(Device, x, true)).ToList());
			}
		}
	}
}
