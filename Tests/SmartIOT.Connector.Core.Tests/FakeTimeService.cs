using System;

namespace SmartIOT.Connector.Core.Tests
{
    internal class FakeTimeService : ITimeService
    {
        public DateTime Now { get; set; }

        public bool IsTimeoutElapsed(DateTime instant, TimeSpan timeout)
        {
            return IsTimeoutElapsed(instant, Now, timeout);
        }

        public bool IsTimeoutElapsed(DateTime from, DateTime to, TimeSpan timeout)
        {
            return to - from >= timeout || to < from;
        }
    }
}
