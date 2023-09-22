namespace SmartIOT.Connector.Core;

public interface ITimeService
{
    DateTime Now { get; }

    bool IsTimeoutElapsed(DateTime instant, TimeSpan timeout);

    bool IsTimeoutElapsed(DateTime from, DateTime to, TimeSpan timeout);
}
