#pragma warning disable S3885 // "Assembly.Load" should be used

using Microsoft.Extensions.DependencyInjection;
using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Factory;
using SmartIOT.Connector.Core.Scheduler;
using System.Reflection;

namespace SmartIOT.Connector.Core;

public class SmartIotConnectorBuilder
{
    private bool _autoDiscoverDeviceDriverFactory;
    private readonly List<IDeviceDriver> _deviceDrivers = new List<IDeviceDriver>();
    private bool _autoDiscoverConnectorFactory;
    private readonly List<IConnector> _connectors = new List<IConnector>();

    public ITimeService TimeService { get; private set; } = new TimeService();
    public ISchedulerFactory SchedulerFactory { get; private set; } = new SchedulerFactory();
    public SmartIotConnectorConfiguration? Configuration { get; private set; }
    public IList<Exception> AutoDiscoveryExceptions { get; } = new List<Exception>();
    public DeviceDriverFactory DeviceDriverFactory { get; } = new DeviceDriverFactory();
    public ConnectorFactory ConnectorFactory { get; } = new ConnectorFactory();

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
        DeviceDriverFactory.Add(factory);
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
        ConnectorFactory.Add(factory);
        return this;
    }

    public SmartIotConnectorBuilder WithConfiguration(SmartIotConnectorConfiguration configuration)
    {
        Configuration = configuration;
        return this;
    }

    public SmartIotConnectorBuilder WithConfigurationJsonFilePath(string jsonFilePath)
    {
        Configuration = SmartIotConnectorConfiguration.FromJson(File.ReadAllText(jsonFilePath));
        return this;
    }

    public SmartIotConnectorBuilder WithSchedulerFactory(ISchedulerFactory schedulerFactory)
    {
        SchedulerFactory = schedulerFactory;
        return this;
    }

    public SmartIotConnectorBuilder WithTimeService(ITimeService timeService)
    {
        TimeService = timeService;
        return this;
    }

    public SmartIotConnector Build(IServiceProvider? serviceProvider = null)
    {
        if (Configuration == null)
            throw new InvalidOperationException("Error building module: Configuration is not set");

        if (_autoDiscoverDeviceDriverFactory)
            DeviceDriverFactory.AddRange(AutoDiscoverDeviceDriverFactories(serviceProvider));

        if (!DeviceDriverFactory.Any() && _deviceDrivers.Count == 0)
            throw new ArgumentException($"No {nameof(IDeviceDriverFactory)} or {nameof(IDeviceDriver)} configured");

        if (_autoDiscoverConnectorFactory)
            ConnectorFactory.AddRange(AutoDiscoverConnectorFactories(serviceProvider));

        var schedulers = BuildSchedulers();
        var connectors = BuildConnectors();

        return new SmartIotConnector(schedulers, connectors, Configuration.SchedulerConfiguration);
    }

    private List<IConnector> BuildConnectors()
    {
        var list = new List<IConnector>();

        foreach (var connectionString in Configuration!.ConnectorConnectionStrings)
        {
            IConnector? connector = ConnectorFactory.CreateConnector(connectionString) ?? throw new ArgumentException($"Error creating connector: ConnectionString {connectionString} not recognized.");
            list.Add(connector);
        }

        list.AddRange(_connectors);

        return list;
    }

    private List<ITagScheduler> BuildSchedulers()
    {
        // creating schedulers from configuration
        var drivers = new Dictionary<DeviceConfiguration, IDeviceDriver>();
        var devices = new List<DeviceConfiguration>(Configuration!.DeviceConfigurations);
        if (devices.Count > 0)
        {
            foreach (var device in devices)
            {
                var driver = DeviceDriverFactory.CreateDriver(device);
                if (driver != null)
                {
                    drivers[device] = driver;
                }
            }

            devices.RemoveAll(x => drivers.ContainsKey(x)); // rimuovo dalla lista temporanea le configurazioni che hanno ritornato un driver
        }

        if (devices.Count > 0)
            throw new ArgumentException($"Error configuring SmartIotConnector: no scheduler factory found for these devices:\r\n{string.Join("\r\n", devices.Select(x => x.Name + ": " + x.ConnectionString))}");

        var schedulers = drivers.Values.Select(x => SchedulerFactory.CreateScheduler(x.Name, x, TimeService, Configuration.SchedulerConfiguration)).ToList();

        // adding schedulers from _deviceDrivers list
        schedulers.AddRange(_deviceDrivers.Select(x => SchedulerFactory.CreateScheduler(x.Name, x, TimeService, Configuration.SchedulerConfiguration)));

        return schedulers;
    }

    private List<IDeviceDriverFactory> AutoDiscoverDeviceDriverFactories(IServiceProvider? serviceProvider)
    {
        var list = new List<IDeviceDriverFactory>();

        foreach (string file in Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll", SearchOption.TopDirectoryOnly))
        {
            try
            {
                Assembly assembly = Assembly.LoadFrom(file);

                foreach (var type in assembly.ExportedTypes)
                {
                    if (typeof(IDeviceDriverFactory).IsAssignableFrom(type)
                        && type != typeof(DeviceDriverFactory)
                        && !DeviceDriverFactory.Any(x => x.GetType() == type) // do not add already added factories
                        && !type.IsAbstract
                        && type.IsClass
                        && !type.IsInterface
                        && type.IsPublic
                        && type.IsVisible)
                    {
                        if (serviceProvider is not null)
                        {
                            list.Add((IDeviceDriverFactory)ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, type));
                        }
                        else
                        {
                            var ctor = type.GetConstructor(Array.Empty<Type>());
                            if (ctor != null)
                                list.Add((IDeviceDriverFactory)ctor.Invoke(Array.Empty<object>()));
                        }
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

    private List<IConnectorFactory> AutoDiscoverConnectorFactories(IServiceProvider? serviceProvider)
    {
        var list = new List<IConnectorFactory>();

        foreach (string file in Directory.EnumerateFiles(AppDomain.CurrentDomain.BaseDirectory, "*.dll", SearchOption.TopDirectoryOnly))
        {
            try
            {
                Assembly assembly = Assembly.LoadFrom(file);

                foreach (var type in assembly.ExportedTypes)
                {
                    if (typeof(IConnectorFactory).IsAssignableFrom(type)
                        && type != typeof(ConnectorFactory)
                        && !ConnectorFactory.Any(x => x.GetType() == type) // do not add already added factories
                        && !type.IsAbstract
                        && type.IsClass
                        && !type.IsInterface
                        && type.IsPublic
                        && type.IsVisible)
                    {
                        if (serviceProvider is not null)
                        {
                            list.Add((IConnectorFactory)ActivatorUtilities.GetServiceOrCreateInstance(serviceProvider, type));
                        }
                        else
                        {
                            var ctor = type.GetConstructor(Array.Empty<Type>());
                            if (ctor != null)
                                list.Add((IConnectorFactory)ctor.Invoke(Array.Empty<object>()));
                        }
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
