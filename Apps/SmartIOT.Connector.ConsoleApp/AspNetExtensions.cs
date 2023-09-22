using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Factory;

namespace SmartIOT.Connector.ConsoleApp;

public static class AspNetExtensions
{
    public static IServiceCollection AddSmartIotConnectorRunner(this IServiceCollection services, AppConfiguration configuration)
    {
        // Add SmartIOT.Connector services to the container.
        services.AddSingleton<AppConfiguration>(configuration);
        services.AddSingleton<SmartIotConnector>(s => s.GetRequiredService<Runner>().SmartIotConnector);
        services.AddSingleton<IConnectorFactory>(s => s.GetRequiredService<Runner>().ConnectorFactory);
        services.AddSingleton<IDeviceDriverFactory>(s => s.GetRequiredService<Runner>().DeviceDriverFactory);
        services.AddSingleton<ISchedulerFactory>(s => s.GetRequiredService<Runner>().SchedulerFactory);
        services.AddSingleton<ITimeService>(s => s.GetRequiredService<Runner>().TimeService);

        services.AddHostedService<Runner>();

        return services;
    }
}
