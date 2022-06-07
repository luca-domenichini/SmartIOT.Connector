using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Factory;
using SmartIOT.Connector.Core.Scheduler;
using System.Reflection;

namespace SmartIOT.Connector.Core
{
	public class SmartIotConnectorBuilder
	{
		private bool _autoDiscoverDeviceDriverFactory;
		private readonly List<IDeviceDriver> _deviceDrivers = new List<IDeviceDriver>();
		private readonly List<IDeviceDriverFactory> _deviceDriverFactories = new List<IDeviceDriverFactory>();
		private bool _autoDiscoverConnectorFactory;
		private readonly List<IConnector> _connectors = new List<IConnector>();
		private readonly List<IConnectorFactory> _connectorFactories = new List<IConnectorFactory>();
		private ITimeService _timeService = new TimeService();
		private ISchedulerFactory _schedulerFactory = new SchedulerFactory();
		private SmartIotConnectorConfiguration? _configuration;
		public IList<Exception> AutoDiscoveryExceptions { get; } = new List<Exception>();

		public SmartIotConnectorBuilder WithAutoDiscoverDeviceDriverFactories(bool value = true)
		{
			_autoDiscoverDeviceDriverFactory = value;
			return this;
		}

		public SmartIotConnectorBuilder AddDeviceDriver(IDeviceDriver deviceDriver)
		{
			_deviceDrivers.Add(deviceDriver);
			return this;
		}

		public SmartIotConnectorBuilder AddDeviceDriverFactory(IDeviceDriverFactory factory)
		{
			_deviceDriverFactories.Add(factory);
			return this;
		}

		public SmartIotConnectorBuilder WithAutoDiscoverConnectorFactories(bool value = true)
		{
			_autoDiscoverConnectorFactory = value;
			return this;
		}

		public SmartIotConnectorBuilder AddConnector(IConnector connector)
		{
			_connectors.Add(connector);
			return this;
		}

		public SmartIotConnectorBuilder AddConnectorFactory(IConnectorFactory factory)
		{
			_connectorFactories.Add(factory);
			return this;
		}

		public SmartIotConnectorBuilder WithConfiguration(SmartIotConnectorConfiguration configuration)
		{
			_configuration = configuration;
			return this;
		}
		public SmartIotConnectorBuilder WithConfigurationJsonFilePath(string jsonFilePath)
		{
			_configuration = SmartIotConnectorConfiguration.FromJson(File.ReadAllText(jsonFilePath));
			return this;
		}

		public SmartIotConnectorBuilder WithSchedulerFactory(ISchedulerFactory schedulerFactory)
		{
			_schedulerFactory = schedulerFactory;
			return this;
		}

		public SmartIotConnectorBuilder WithTimeService(ITimeService timeService)
		{
			_timeService = timeService;
			return this;
		}

		public SmartIotConnector Build()
		{
			if (_configuration == null)
				throw new InvalidOperationException("Error building module: Configuration is not set");

			if (_autoDiscoverDeviceDriverFactory)
				_deviceDriverFactories.AddRange(AutoDiscoverDeviceDriverFactories());

			if (!_deviceDriverFactories.Any() && !_deviceDrivers.Any())
				throw new ArgumentException($"Nessuna {nameof(IDeviceDriverFactory)} o {nameof(IDeviceDriver)} presente in configurazione");

			if (_autoDiscoverConnectorFactory)
				_connectorFactories.AddRange(AutoDiscoverConnectorFactory());


			IList<ITagScheduler> schedulers = BuildSchedulers();
			IList<IConnector> connectors = BuildConnectors();

			foreach (var connector in connectors)
			{
				foreach (var scheduler in schedulers)
				{
					scheduler.AddConnector(connector);
				}
			}

			return new SmartIotConnector(schedulers, connectors);
		}

		private IList<IConnector> BuildConnectors()
		{
			var list = new List<IConnector>();

			foreach (var connectionString in _configuration!.ConnectorConnectionStrings)
			{
				IConnector? connector = null;

				foreach (var factory in _connectorFactories)
				{
					connector = factory.CreateConnector(connectionString);
					if (connector != null)
						break;
				}

				if (connector == null)
					throw new ArgumentException($"Impossibile creare il connector: ConnectionString {connectionString} non riconosciuta.");

				list.Add(connector);
			}

			list.AddRange(_connectors);

			return list;
		}

		private IList<ITagScheduler> BuildSchedulers()
		{
			// creating schedulers from configuration
			var drivers = new Dictionary<DeviceConfiguration, IDeviceDriver>();
			var devices = new List<DeviceConfiguration>(_configuration!.DeviceConfigurations);
			if (devices.Any())
			{

				foreach (var factory in _deviceDriverFactories)
				{
					var dictionary = factory.CreateDrivers(devices);

					devices.RemoveAll(x => dictionary.ContainsKey(x)); // rimuovo dalla lista temporanea le configurazioni che hanno ritornato un driver

					foreach (var kv in dictionary)
					{
						drivers[kv.Key] = kv.Value;
					}

					if (!devices.Any())
						break;
				}
			}

			if (devices.Any())
				throw new ArgumentException($"Error configuring SmartIotConnector: no scheduler factory found for these devices:\r\n{string.Join("\r\n", devices.Select(x => x.Name + ": " + x.ConnectionString))}");

			var schedulers = drivers.Values.Select(x => _schedulerFactory.CreateScheduler(x.Name, x, _timeService, _configuration.SchedulerConfiguration)).ToList();

			// adding schedulers from _deviceDrivers list
			schedulers.AddRange(_deviceDrivers.Select(x => _schedulerFactory.CreateScheduler(x.Name, x, _timeService, _configuration.SchedulerConfiguration)));

			return schedulers;
		}

		private IList<IDeviceDriverFactory> AutoDiscoverDeviceDriverFactories()
		{
			var list = new List<IDeviceDriverFactory>();

			foreach (string file in Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll", SearchOption.TopDirectoryOnly))
			{
				try
				{
					Assembly assembly = Assembly.LoadFrom(file);

					foreach (var type in assembly.ExportedTypes)
					{
						// le factory già presenti in elenco non li aggiungiamo nuovamente
						bool alreadyAvailable = _deviceDriverFactories.Any(x => x.GetType() == type);
						if (!alreadyAvailable
							&& typeof(IDeviceDriverFactory).IsAssignableFrom(type)
							&& !type.IsAbstract
							&& type.IsClass
							&& !type.IsInterface
							&& type.IsPublic
							&& type.IsVisible)
						{
							var ctor = type.GetConstructor(Array.Empty<Type>());
							if (ctor != null)
								list.Add((IDeviceDriverFactory)ctor.Invoke(Array.Empty<object>()));
						}
					}
				}
				catch (Exception ex) when (
					ex is BadImageFormatException
					|| ex is TypeLoadException
					|| ex is FileLoadException
					|| ex is FileNotFoundException)
				{
					AutoDiscoveryExceptions.Add(ex);
				}
			}

			return list;
		}

		private IList<IConnectorFactory> AutoDiscoverConnectorFactory()
		{
			var list = new List<IConnectorFactory>();

			foreach (string file in Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll", SearchOption.TopDirectoryOnly))
			{
				try
				{
					Assembly assembly = Assembly.LoadFrom(file);

					foreach (var type in assembly.ExportedTypes)
					{
						// le factory già presenti in elenco non li aggiungiamo nuovamente
						bool alreadyAvailable = _connectorFactories.Any(x => x.GetType() == type);
						if (!alreadyAvailable
							&& typeof(IConnectorFactory).IsAssignableFrom(type)
							&& !type.IsAbstract
							&& type.IsClass
							&& !type.IsInterface
							&& type.IsPublic
							&& type.IsVisible)
						{
							var ctor = type.GetConstructor(Array.Empty<Type>());
							if (ctor != null)
								list.Add((IConnectorFactory)ctor.Invoke(Array.Empty<object>()));
						}
					}
				}
				catch (Exception ex) when (
					ex is BadImageFormatException
					|| ex is TypeLoadException
					|| ex is FileLoadException
					|| ex is FileNotFoundException)
				{
					AutoDiscoveryExceptions.Add(ex);
				}
			}

			return list;
		}

	}
}
