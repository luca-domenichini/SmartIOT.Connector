using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Events;
using SmartIOT.Connector.Prometheus;

namespace SmartIOT.Connector.Runner.Console
{
	public class Runner
	{
		private readonly ManualResetEventSlim _stopEvent = new ManualResetEventSlim(false);
		public SmartIotConnector SmartIotConnector { get; init; }

		public Runner(RunnerConfiguration configuration, Action<IList<Exception>>? onExceptionDuringDiscovery = null)
		{
			if (configuration.Configuration == null)
				throw new ArgumentException($"SmartIOT.Connector Configuration not valid");

			SmartIotConnectorBuilder builder = new SmartIotConnectorBuilder();
			SmartIotConnector = builder
				.WithAutoDiscoverDeviceDriverFactories()
				.WithAutoDiscoverConnectorFactories()
				.WithConfiguration(configuration.Configuration)
				.Build();

			if (configuration.PrometheusConfiguration != null)
				SmartIotConnector.AddPrometheus(configuration.PrometheusConfiguration);

			if (builder.AutoDiscoveryExceptions.Any())
				onExceptionDuringDiscovery?.Invoke(builder.AutoDiscoveryExceptions);
		}

		public void Run(EventHandler<EventArgs>? onStartingHandler = null
			, EventHandler<EventArgs>? onStartedHandler = null
			, EventHandler<EventArgs>? onStoppingHandler = null
			, EventHandler<EventArgs>? onStoppedHandler = null
			, EventHandler<ExceptionEventArgs>? onExceptionHandler = null
			, EventHandler<TagScheduleEventArgs>? onTagRead = null
			, EventHandler<TagScheduleEventArgs>? onTagWrite = null
			, EventHandler<DeviceDriverRestartingEventArgs>? onSchedulerRestarting = null
			, EventHandler<DeviceDriverRestartedEventArgs>? onSchedulerRestarted = null
			, EventHandler<ConnectorStartedEventArgs>? onConnectorStartedHandler = null
			, EventHandler<ConnectorStoppedEventArgs>? onConnectorStoppedHandler = null
			, EventHandler<ConnectorConnectedEventArgs>? onConnectorConnectedHandler = null
			, EventHandler<ConnectorDisconnectedEventArgs>? onConnectorDisconnectedHandler = null
			, EventHandler<ConnectorExceptionEventArgs>? onConnectorExceptionHandler = null
			)
		{
			System.Console.CancelKeyPress += (s, e) =>
			{
				e.Cancel = true;
				_stopEvent.Set();
			};

			SmartIotConnector.SchedulerRestarting += onSchedulerRestarting;
			SmartIotConnector.SchedulerRestarted += onSchedulerRestarted;
			SmartIotConnector.TagReadEvent += onTagRead;
			SmartIotConnector.TagWriteEvent += onTagWrite;
			SmartIotConnector.ExceptionHandler += onExceptionHandler;
			SmartIotConnector.Starting += onStartingHandler;
			SmartIotConnector.Started += onStartedHandler;
			SmartIotConnector.Stopping += onStoppingHandler;
			SmartIotConnector.Stopped += onStoppedHandler;
			SmartIotConnector.ConnectorStarted += onConnectorStartedHandler;
			SmartIotConnector.ConnectorStopped += onConnectorStoppedHandler;
			SmartIotConnector.ConnectorConnected += onConnectorConnectedHandler;
			SmartIotConnector.ConnectorDisconnected += onConnectorDisconnectedHandler;
			SmartIotConnector.ConnectorException += onConnectorExceptionHandler;

			SmartIotConnector.Start();
		}

		public void RunAndWaitForShutdown(EventHandler<EventArgs>? onStartingHandler = null
			, EventHandler<EventArgs>? onStartedHandler = null
			, EventHandler<EventArgs>? onStoppingHandler = null
			, EventHandler<EventArgs>? onStoppedHandler = null
			, EventHandler<ExceptionEventArgs>? onExceptionHandler = null
			, EventHandler<TagScheduleEventArgs>? onTagRead = null
			, EventHandler<TagScheduleEventArgs>? onTagWrite = null
			, EventHandler<DeviceDriverRestartingEventArgs>? onSchedulerRestarting = null
			, EventHandler<DeviceDriverRestartedEventArgs>? onSchedulerRestarted = null
			, EventHandler<ConnectorStartedEventArgs>? onConnectorStartedHandler = null
			, EventHandler<ConnectorStoppedEventArgs>? onConnectorStoppedHandler = null
			, EventHandler<ConnectorConnectedEventArgs>? onConnectorConnectedHandler = null
			, EventHandler<ConnectorDisconnectedEventArgs>? onConnectorDisconnectedHandler = null
			, EventHandler<ConnectorExceptionEventArgs>? onConnectorExceptionHandler = null
			)
		{
			Run(onStartingHandler
				, onStartedHandler
				, onStoppingHandler
				, onStoppedHandler
				, onExceptionHandler
				, onTagRead
				, onTagWrite
				, onSchedulerRestarting
				, onSchedulerRestarted
				, onConnectorStartedHandler
				, onConnectorStoppedHandler
				, onConnectorConnectedHandler
				, onConnectorDisconnectedHandler
				, onConnectorExceptionHandler
				);

			WaitForShutdown();
		}

		public void WaitForShutdown()
		{
			_stopEvent.Wait();
			SmartIotConnector.Stop();
		}
	}
}
