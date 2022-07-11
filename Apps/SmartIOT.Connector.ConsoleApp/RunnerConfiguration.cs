using SmartIOT.Connector.Core;
using SmartIOT.Connector.Prometheus;

namespace SmartIOT.Connector.ConsoleApp
{
	public class RunnerConfiguration
	{
		public SmartIotConnectorConfiguration? Configuration { get; set; }
		public PrometheusConfiguration? PrometheusConfiguration { get; set; }
		public LogConfiguration? LogConfiguration { get; set; }
	}
}
