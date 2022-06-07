using System.Text.Json.Serialization;

namespace SmartIOT.Connector.Core.Conf
{
	public class SchedulerConfiguration
	{
		public int MaxErrorsBeforeReconnection { get; set; } = 10;
		public int RestartDeviceInErrorTimeoutMillis { get; set; } = (int)TimeSpan.FromSeconds(30).TotalMilliseconds;
		[JsonIgnore]
		public TimeSpan RestarDeviceInErrorTimeout => TimeSpan.FromMilliseconds(RestartDeviceInErrorTimeoutMillis);
		public int WaitTimeAfterErrorMillis { get; set; } = (int)TimeSpan.FromSeconds(1).TotalMilliseconds;
		[JsonIgnore]
		public TimeSpan WaitTimeAfterError => TimeSpan.FromMilliseconds(WaitTimeAfterErrorMillis);
		public int WaitTimeBetweenEveryScheduleMillis { get; set; } = 0;
		[JsonIgnore]
		public TimeSpan WaitTimeBetweenEverySchedule => TimeSpan.FromMilliseconds(WaitTimeBetweenEveryScheduleMillis);
		public int WaitTimeBetweenReadSchedulesMillis { get; set; } = 0;
		[JsonIgnore]
		public TimeSpan WaitTimeBetweenReadSchedules => TimeSpan.FromMilliseconds(WaitTimeBetweenReadSchedulesMillis);
	}
}
