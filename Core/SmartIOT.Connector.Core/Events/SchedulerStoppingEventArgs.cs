using SmartIOT.Connector.Core.Scheduler;

namespace SmartIOT.Connector.Core.Events
{
    public class SchedulerStoppingEventArgs : EventArgs
    {
        public ITagScheduler Scheduler { get; }

        public SchedulerStoppingEventArgs(ITagScheduler scheduler)
        {
            Scheduler = scheduler;
        }
    }
}
