using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Factory;
using SmartIOT.Connector.Prometheus;

namespace SmartIOT.Connector.ConsoleApp
{
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

            SmartIotConnector.SchedulerStarting += (s, e) => _logger.LogInformation($"{e.Scheduler.DeviceDriver.Name}: Scheduler starting");
            SmartIotConnector.SchedulerStopping += (s, e) => _logger.LogInformation($"{e.Scheduler.DeviceDriver.Name}: Scheduler stopping");
            SmartIotConnector.SchedulerRestarting += (s, e) => _logger.LogInformation($"{e.DeviceDriver.Name}: Scheduler restarting");
            SmartIotConnector.SchedulerRestarted += (s, e) =>
            {
                if (e.IsSuccess)
                    _logger.LogInformation($"{e.DeviceDriver.Name}: Scheduler restarted successfully");
                else
                    _logger.LogError($"{e.DeviceDriver.Name}: Error during scheduler restart: {e.ErrorDescription}");
            };
            SmartIotConnector.TagReadEvent += (s, e) =>
            {
                if (e.TagScheduleEvent.Data != null)
                {
                    // data event
                    if (e.TagScheduleEvent.Data.Length > 0)
                        _logger.LogInformation($"{e.DeviceDriver.Name}: {e.TagScheduleEvent.Device.Name}, {e.TagScheduleEvent.Tag.TagId}: received data[{e.TagScheduleEvent.StartOffset}..{e.TagScheduleEvent.StartOffset + e.TagScheduleEvent.Data.Length - 1}], size {e.TagScheduleEvent.Data.Length}");
                }
                else if (e.TagScheduleEvent.IsErrorNumberChanged)
                {
                    // status changed
                    _logger.LogInformation($"{e.DeviceDriver.Name}: {e.TagScheduleEvent.Device.Name}, {e.TagScheduleEvent.Tag.TagId}: status changed {e.TagScheduleEvent.ErrorNumber} {e.TagScheduleEvent.Description}");
                }
            };
            SmartIotConnector.TagWriteEvent += (s, e) =>
            {
            };
            SmartIotConnector.ExceptionHandler += (s, e) => _logger.LogError(e.Exception, $"Exception caught: {e.Exception.Message}");
            SmartIotConnector.Starting += (s, e) => _logger.LogInformation("SmartIOT.Connector starting..");
            SmartIotConnector.Started += (s, e) => _logger.LogInformation("SmartIOT.Connector started. Press Ctrl-C for graceful stop.");
            SmartIotConnector.Stopping += (s, e) => _logger.LogInformation("SmartIOT.Connector stopping..");
            SmartIotConnector.Stopped += (s, e) => _logger.LogInformation("SmartIOT.Connector stopped");
            SmartIotConnector.ConnectorStarted += (s, e) =>
            {
                _logger.LogInformation($"{e.Connector.GetType().Name}: {e.Info}");
            };
            SmartIotConnector.ConnectorStopped += (s, e) =>
            {
                _logger.LogInformation($"{e.Connector.GetType().Name}: {e.Info}");
            };
            SmartIotConnector.ConnectorConnected += (s, e) =>
            {
                _logger.LogInformation($"{e.Connector.GetType().Name}: {e.Info}");
            };
            SmartIotConnector.ConnectorConnectionFailed += (s, e) =>
            {
                _logger.LogError(e.Exception, $"{e.Connector.GetType().Name}: {e.Info}");
            };
            SmartIotConnector.ConnectorDisconnected += (s, e) =>
            {
                _logger.LogInformation($"{e.Connector.GetType().Name}: {e.Info}");
            };
            SmartIotConnector.ConnectorException += (s, e) =>
            {
                _logger.LogError(e.Exception, $"{e.Connector.GetType().Name}: Unexpected exception: {e.Exception.Message}");
            };

            if (builder.AutoDiscoveryExceptions.Any())
            {
                _logger.LogWarning($"Error autodiscoverying dll: [{Environment.NewLine}{string.Join($"{Environment.NewLine}\t", builder.AutoDiscoveryExceptions.Select(x => x.Message))}{Environment.NewLine}]");
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
}
