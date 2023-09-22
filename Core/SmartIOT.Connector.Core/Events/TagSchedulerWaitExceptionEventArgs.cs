using SmartIOT.Connector.Core.Scheduler;

namespace SmartIOT.Connector.Core.Events;

public class TagSchedulerWaitExceptionEventArgs : EventArgs
{
    public TimeSpan WaitTime { get; }

    public TagSchedulerWaitExceptionEventArgs(TagSchedulerWaitException exception)
    {
        WaitTime = exception.WaitTime;
    }
}
