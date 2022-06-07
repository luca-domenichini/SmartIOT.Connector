using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Scheduler;

namespace SmartIOT.Connector.Core.Factory
{
	public interface ISchedulerFactory
	{
		ITagScheduler CreateScheduler(string name, IDeviceDriver deviceDriver, ITimeService timeService, SchedulerConfiguration configuration);
	}
}
