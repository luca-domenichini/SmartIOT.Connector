using Prometheus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmartIOT.Connector.Prometheus
{
	public class PrometheusConfiguration
	{
		public string HostName { get; }
		public int Port { get; }
		public string Url { get; }
		public string MetricsPrefix { get; }
		public CollectorRegistry? CollectorRegistry { get; }
		public bool UseHttps { get; }

		public PrometheusConfiguration(string hostName, int port, string url = "metrics/", string metricsPrefix = "smartiot_connector", CollectorRegistry? collectorRegistry = null, bool useHttps = false)
		{
			HostName = hostName;
			Port = port;
			Url = url;
			MetricsPrefix = metricsPrefix;
			CollectorRegistry = collectorRegistry;
			UseHttps = useHttps;
		}
	}
}
