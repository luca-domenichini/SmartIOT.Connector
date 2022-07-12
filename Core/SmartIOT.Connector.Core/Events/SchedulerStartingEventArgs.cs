using SmartIOT.Connector.Core.Scheduler;

namespace SmartIOT.Connector.Core.Events
{
	public class SchedulerStartingEventArgs : EventArgs
	{
		public ITagScheduler Scheduler { get; }

		public SchedulerStartingEventArgs(ITagScheduler scheduler)
		{
			Scheduler = scheduler;
		}
	}
}
