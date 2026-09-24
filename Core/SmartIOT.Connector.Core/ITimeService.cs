namespace SmartIOT.Connector.Core;

public interface ITimeService
{
    /// <summary>
    /// Current instant expressed in ticks (1 tick = 100 nanoseconds).
    /// Tick values are stored directly on tags and schedulers, so that time arithmetic
    /// is plain long math instead of DateTime math, and so that cross-thread reads and
    /// writes can be made volatile/atomic.
    /// </summary>
    long NowTicks { get; }

    bool IsTimeoutElapsed(long instant, TimeSpan timeout);

    bool IsTimeoutElapsed(long from, long to, TimeSpan timeout);
}
