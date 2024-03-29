﻿namespace SmartIOT.Connector.Prometheus;

public class PrometheusConfiguration
{
    public string HostName { get; }
    public int Port { get; }
    public string Url { get; }
    public string MetricsPrefix { get; }
    public bool UseHttps { get; }

    public PrometheusConfiguration(string hostName, int port, string url = "metrics/", string metricsPrefix = "smartiot_connector", bool useHttps = false)
    {
        HostName = hostName;
        Port = port;
        Url = url;
        MetricsPrefix = metricsPrefix;
        UseHttps = useHttps;
    }
}
