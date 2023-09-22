using Prometheus;

namespace SmartIOT.Connector.Prometheus
{
    public static class PrometheusExtensions
    {
        private static Core.SmartIotConnector AddPrometheusServerImpl(this Core.SmartIotConnector connector, IMetricServer server, bool isManagedServer, string metricsPrefix)
        {
            var extension = new ExtensionImpl(server, isManagedServer, metricsPrefix);

            connector.Starting += (s, e) => extension.OnStarting(connector);
            connector.Stopping += (s, e) => extension.OnStopping(connector);

            return connector;
        }

        public static Core.SmartIotConnector AddUnmanagedPrometheusServer(this Core.SmartIotConnector connector, IMetricServer server, string metricsPrefix = "smartiot_connector")
        {
            return connector.AddPrometheusServerImpl(server, false, metricsPrefix);
        }

        public static Core.SmartIotConnector AddManagedPrometheusServer(this Core.SmartIotConnector connector, int port, string url = "metrics/", bool useHttps = false, string metricsPrefix = "smartiot_connector")
        {
            return connector.AddPrometheusServerImpl(new MetricServer(port, url, null, useHttps), true, metricsPrefix);
        }

        public static Core.SmartIotConnector AddManagedPrometheusServer(this Core.SmartIotConnector connector, string hostname, int port, string url = "metrics/", bool useHttps = false, string metricsPrefix = "smartiot_connector")
        {
            return connector.AddPrometheusServerImpl(new MetricServer(hostname, port, url, null, useHttps), true, metricsPrefix);
        }

        public static Core.SmartIotConnector AddPrometheus(this Core.SmartIotConnector connector, PrometheusConfiguration prometheusConfiguration)
        {
            if (prometheusConfiguration.Port > 0)
            {
                if (!string.IsNullOrWhiteSpace(prometheusConfiguration.HostName))
                    return connector.AddPrometheusServerImpl(new MetricServer(prometheusConfiguration.HostName, prometheusConfiguration.Port, prometheusConfiguration.Url, null, prometheusConfiguration.UseHttps), true, prometheusConfiguration.MetricsPrefix);
                else
                    return connector.AddPrometheusServerImpl(new MetricServer(prometheusConfiguration.Port, prometheusConfiguration.Url, null, prometheusConfiguration.UseHttps), true, prometheusConfiguration.MetricsPrefix);
            }

            return connector;
        }
    }
}