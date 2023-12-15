using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SmartIOT.Connector.Core;

namespace SmartIOT.Connector.DependencyInjection;

public class SmartIotConnectorHostedService : IHostedService
{
    private readonly SmartIotConnector _smartIotConnector;
    private readonly ILogger<SmartIotConnectorHostedService> _logger;

    public SmartIotConnectorHostedService(SmartIotConnector smartIotConnector, SmartIotConnectorBuilder builder, ILogger<SmartIotConnectorHostedService> logger)
    {
        _smartIotConnector = smartIotConnector;
        _logger = logger;

        _smartIotConnector.SchedulerStarting += (s, e) => _logger.LogInformation("{driver}: Scheduler starting", e.Scheduler.DeviceDriver.Name);
        _smartIotConnector.SchedulerStopping += (s, e) => _logger.LogInformation("{driver}: Scheduler stopping", e.Scheduler.DeviceDriver.Name);
        _smartIotConnector.SchedulerRestarting += (s, e) => _logger.LogInformation("{driver}: Scheduler restarting", e.DeviceDriver.Name);
        _smartIotConnector.SchedulerRestarted += (s, e) =>
        {
            if (e.IsSuccess)
                _logger.LogInformation("{driver}: Scheduler restarted successfully", e.DeviceDriver.Name);
            else
                _logger.LogError("{driver}: Error during scheduler restart: {message}", e.DeviceDriver.Name, e.ErrorDescription);
        };
        _smartIotConnector.TagReadEvent += (s, e) =>
        {
            if (e.TagScheduleEvent.Data != null)
            {
                // data event
                if (_logger.IsEnabled(LogLevel.Debug) && e.TagScheduleEvent.Data.Length > 0)
                    _logger.LogDebug("{driver}: {device}, {tag}: received data[{offset}..{endOffset}], size {size}", e.DeviceDriver.Name, e.TagScheduleEvent.Device.Name, e.TagScheduleEvent.Tag.TagId, e.TagScheduleEvent.StartOffset, e.TagScheduleEvent.StartOffset + e.TagScheduleEvent.Data.Length - 1, e.TagScheduleEvent.Data.Length);
            }
            else if (e.TagScheduleEvent.IsErrorNumberChanged)
            {
                // status changed
                _logger.LogWarning("{driver}: {device}, {tag}: status changed {err} {message}", e.DeviceDriver.Name, e.TagScheduleEvent.Device.Name, e.TagScheduleEvent.Tag.TagId, e.TagScheduleEvent.ErrorNumber, e.TagScheduleEvent.Description);
            }
        };
        _smartIotConnector.TagWriteEvent += (s, e) =>
        {
            if (e.TagScheduleEvent.Data is not null && _logger.IsEnabled(LogLevel.Debug))
                _logger.LogDebug("{driver}: {device}, {tag}: written data[{offset}..{endOffset}], size {size}", e.DeviceDriver.Name, e.TagScheduleEvent.Device.Name, e.TagScheduleEvent.Tag.TagId, e.TagScheduleEvent.StartOffset, e.TagScheduleEvent.StartOffset + e.TagScheduleEvent.Data.Length - 1, e.TagScheduleEvent.Data.Length);
        };
        _smartIotConnector.ExceptionHandler += (s, e) => _logger.LogError(e.Exception, "Unexpected exception caught: {message}", e.Exception.Message);
        _smartIotConnector.Starting += (s, e) => _logger.LogInformation("SmartIOT.Connector starting..");
        _smartIotConnector.Started += (s, e) => _logger.LogInformation("SmartIOT.Connector started. Press Ctrl-C for graceful stop.");
        _smartIotConnector.Stopping += (s, e) => _logger.LogInformation("SmartIOT.Connector stopping..");
        _smartIotConnector.Stopped += (s, e) => _logger.LogInformation("SmartIOT.Connector stopped");
        _smartIotConnector.ConnectorStarted += (s, e) =>
        {
            _logger.LogInformation("{connector}: {message}", e.Connector.GetType().Name, e.Info);
        };
        _smartIotConnector.ConnectorStopped += (s, e) =>
        {
            _logger.LogInformation("{connector}: {message}", e.Connector.GetType().Name, e.Info);
        };
        _smartIotConnector.ConnectorConnected += (s, e) =>
        {
            _logger.LogInformation("{connector}: {message}", e.Connector.GetType().Name, e.Info);
        };
        _smartIotConnector.ConnectorConnectionFailed += (s, e) =>
        {
            _logger.LogWarning(e.Exception, "{connector}: {message}", e.Connector.GetType().Name, e.Info);
        };
        _smartIotConnector.ConnectorDisconnected += (s, e) =>
        {
            _logger.LogInformation("{connector}: {message}", e.Connector.GetType().Name, e.Info);
        };
        _smartIotConnector.ConnectorException += (s, e) =>
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
        return _smartIotConnector.StartAsync();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return _smartIotConnector.StopAsync();
    }
}
