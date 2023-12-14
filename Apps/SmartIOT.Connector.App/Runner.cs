using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Factory;
using SmartIOT.Connector.Prometheus;

namespace SmartIOT.Connector.App;

public class Runner : IHostedService
{
    private readonly ILogger<Runner> _logger;

    public SmartIotConnector SmartIotConnector { get; }
    public ConnectorFactory ConnectorFactory { get; }
    public DeviceDriverFactory DeviceDriverFactory { get; }
    public ISchedulerFactory SchedulerFactory { get; }
    public ITimeService TimeService { get; }

    public Runner(AppConfiguration configuration, ILogger<Runner> logger)
    {
        _logger = logger;

        if (configuration.Configuration == null)
            throw new ArgumentException($"SmartIOT.Connector Configuration not valid");

        SmartIotConnectorBuilder builder = new SmartIotConnectorBuilder();
        SmartIotConnector = builder
            .WithAutoDiscoverDeviceDriverFactories()
            .WithAutoDiscoverConnectorFactories()
            .WithConfiguration(configuration.Configuration)
            .Build();

        ConnectorFactory = builder.ConnectorFactory;
        DeviceDriverFactory = builder.DeviceDriverFactory;
        SchedulerFactory = builder.SchedulerFactory;
        TimeService = builder.TimeService;

        if (configuration.PrometheusConfiguration != null)
            SmartIotConnector.AddPrometheus(configuration.PrometheusConfiguration);

        SmartIotConnector.SchedulerStarting += (s, e) => _logger.LogInformation("{driver}: Scheduler starting", e.Scheduler.DeviceDriver.Name);
        SmartIotConnector.SchedulerStopping += (s, e) => _logger.LogInformation("{driver}: Scheduler stopping", e.Scheduler.DeviceDriver.Name);
        SmartIotConnector.SchedulerRestarting += (s, e) => _logger.LogInformation("{driver}: Scheduler restarting", e.DeviceDriver.Name);
        SmartIotConnector.SchedulerRestarted += (s, e) =>
        {
            if (e.IsSuccess)
                _logger.LogInformation("{driver}: Scheduler restarted successfully", e.DeviceDriver.Name);
            else
                _logger.LogError("{driver}: Error during scheduler restart: {message}", e.DeviceDriver.Name, e.ErrorDescription);
        };
        SmartIotConnector.TagReadEvent += (s, e) =>
        {
            if (e.TagScheduleEvent.Data != null)
            {
                // data event
                if (e.TagScheduleEvent.Data.Length > 0)
                    _logger.LogInformation("{driver}: {device}, {tag}: received data[{offset}..{endOffset}], size {size}", e.DeviceDriver.Name, e.TagScheduleEvent.Device.Name, e.TagScheduleEvent.Tag.TagId, e.TagScheduleEvent.StartOffset, e.TagScheduleEvent.StartOffset + e.TagScheduleEvent.Data.Length - 1, e.TagScheduleEvent.Data.Length);
            }
            else if (e.TagScheduleEvent.IsErrorNumberChanged)
            {
                // status changed
                _logger.LogInformation("{driver}: {device}, {tag}: status changed {err} {message}", e.DeviceDriver.Name, e.TagScheduleEvent.Device.Name, e.TagScheduleEvent.Tag.TagId, e.TagScheduleEvent.ErrorNumber, e.TagScheduleEvent.Description);
            }
        };
        SmartIotConnector.TagWriteEvent += (s, e) =>
        {
        };
        SmartIotConnector.ExceptionHandler += (s, e) => _logger.LogError(e.Exception, "Exception caught: {message}", e.Exception.Message);
        SmartIotConnector.Starting += (s, e) => _logger.LogInformation("SmartIOT.Connector starting..");
        SmartIotConnector.Started += (s, e) => _logger.LogInformation("SmartIOT.Connector started. Press Ctrl-C for graceful stop.");
        SmartIotConnector.Stopping += (s, e) => _logger.LogInformation("SmartIOT.Connector stopping..");
        SmartIotConnector.Stopped += (s, e) => _logger.LogInformation("SmartIOT.Connector stopped");
        SmartIotConnector.ConnectorStarted += (s, e) =>
        {
            _logger.LogInformation("{connector}: {message}", e.Connector.GetType().Name, e.Info);
        };
        SmartIotConnector.ConnectorStopped += (s, e) =>
        {
            _logger.LogInformation("{connector}: {message}", e.Connector.GetType().Name, e.Info);
        };
        SmartIotConnector.ConnectorConnected += (s, e) =>
        {
            _logger.LogInformation("{connector}: {message}", e.Connector.GetType().Name, e.Info);
        };
        SmartIotConnector.ConnectorConnectionFailed += (s, e) =>
        {
            _logger.LogError(e.Exception, "{connector}: {message}", e.Connector.GetType().Name, e.Info);
        };
        SmartIotConnector.ConnectorDisconnected += (s, e) =>
        {
            _logger.LogInformation("{connector}: {message}", e.Connector.GetType().Name, e.Info);
        };
        SmartIotConnector.ConnectorException += (s, e) =>
        {
            _logger.LogError(e.Exception, "{connector}: Unexpected exception: {message}", e.Connector.GetType().Name, e.Exception.Message);
        };

        if (builder.AutoDiscoveryExceptions.Any() && _logger.IsEnabled(LogLevel.Debug))
        {
            string details = string.Join($"{Environment.NewLine}\t", builder.AutoDiscoveryExceptions.Select(x => x.Message));
            _logger.LogDebug("Error autodiscoverying dll: [{nl}{details}{nl}]", Environment.NewLine, details, Environment.NewLine);
        }
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        return SmartIotConnector.StartAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return SmartIotConnector.StopAsync();
    }
}
