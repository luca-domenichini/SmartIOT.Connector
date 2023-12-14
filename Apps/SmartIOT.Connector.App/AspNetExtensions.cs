using SmartIOT.Connector.Core.Factory;

namespace SmartIOT.Connector.App;

public static class AspNetExtensions
{
    public static IServiceCollection AddSmartIotConnectorRunner(this IServiceCollection services, AppConfiguration configuration)
    {
        // Add SmartIOT.Connector services to the container.
        services.AddSingleton(configuration);
        services.AddSingleton(s => s.GetRequiredService<Runner>().SmartIotConnector);
        services.AddSingleton<IConnectorFactory>(s => s.GetRequiredService<Runner>().ConnectorFactory);
        services.AddSingleton<IDeviceDriverFactory>(s => s.GetRequiredService<Runner>().DeviceDriverFactory);
        services.AddSingleton(s => s.GetRequiredService<Runner>().SchedulerFactory);
        services.AddSingleton(s => s.GetRequiredService<Runner>().TimeService);

        // add custom Runner as a singleton and as a hosted service
        services.AddSingleton<Runner>();
        services.AddHostedService(sp => sp.GetRequiredService<Runner>());

        return services;
    }
}
