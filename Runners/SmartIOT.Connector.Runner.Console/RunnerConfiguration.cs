using SmartIOT.Connector.Core;
using SmartIOT.Connector.Prometheus;

namespace SmartIOT.Connector.Runner.Console
{
	public class RunnerConfiguration
	{
		public SmartIotConnectorConfiguration? Configuration { get; set; }
		public PrometheusConfiguration? PrometheusConfiguration { get; set; }
	}
}
