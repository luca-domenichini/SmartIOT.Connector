namespace SmartIOT.Connector.Core;

public class TimeService : ITimeService
{
    // Local wall-clock ticks are kept on purpose: values derived from them (e.g. Tag.LastErrorTicks)
    // are surfaced through the REST API as local date/times, so switching to a monotonic or UTC
    // source here would silently change what API consumers observe.
    public long NowTicks => DateTime.Now.Ticks;

    public bool IsTimeoutElapsed(long instant, TimeSpan timeout)
    {
        return IsTimeoutElapsed(instant, NowTicks, timeout);
    }

    public bool IsTimeoutElapsed(long from, long to, TimeSpan timeout)
    {
        return to - from >= timeout.Ticks || to < from;
    }
}
