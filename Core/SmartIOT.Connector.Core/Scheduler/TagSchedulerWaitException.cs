namespace SmartIOT.Connector.Core.Scheduler;

public class TagSchedulerWaitException : Exception
{
    public TimeSpan WaitTime { get; }

    public TagSchedulerWaitException(TimeSpan waitTime)
    {
        WaitTime = waitTime;
    }
}
