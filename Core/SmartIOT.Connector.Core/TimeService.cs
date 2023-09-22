namespace SmartIOT.Connector.Core;

public class TimeService : ITimeService
{
    public DateTime Now => DateTime.Now;

    public bool IsTimeoutElapsed(DateTime instant, TimeSpan timeout)
    {
        return IsTimeoutElapsed(instant, Now, timeout);
    }

    public bool IsTimeoutElapsed(DateTime from, DateTime to, TimeSpan timeout)
    {
        return to - from >= timeout || to < from;
    }
}
