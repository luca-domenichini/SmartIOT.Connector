﻿using SmartIOT.Connector.Core;
using SmartIOT.Connector.Prometheus;

namespace SmartIOT.Connector.App;

public class AppConfiguration
{
    public SmartIotConnectorConfiguration? Configuration { get; set; }
    public PrometheusConfiguration? PrometheusConfiguration { get; set; }
}
