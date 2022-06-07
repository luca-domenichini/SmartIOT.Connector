using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Scheduler;

namespace SmartIOT.Connector.Core.Factory
{
	public class SchedulerFactory : ISchedulerFactory
	{
		public ITagScheduler CreateScheduler(string name, IDeviceDriver deviceDriver, ITimeService timeService, SchedulerConfiguration configuration)
		{
			return new TagScheduler(name, new TagSchedulerEngine(deviceDriver, timeService, configuration), timeService);
		}
	}
}
