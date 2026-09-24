namespace SmartIOT.Connector.Core.Events;

public class TagSchedulerWaitingEventArgs : EventArgs
{
    public TimeSpan WaitTime { get; }

    public TagSchedulerWaitingEventArgs(TimeSpan waitTime)
    {
        WaitTime = waitTime;
    }
}
