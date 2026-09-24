using System;

namespace SmartIOT.Connector.Core.Tests;

internal class FakeTimeService : ITimeService
{
    public long NowTicks { get; set; }

    public bool IsTimeoutElapsed(long instant, TimeSpan timeout)
    {
        return IsTimeoutElapsed(instant, NowTicks, timeout);
    }

    public bool IsTimeoutElapsed(long from, long to, TimeSpan timeout)
    {
        return to - from >= timeout.Ticks || to < from;
    }
}
