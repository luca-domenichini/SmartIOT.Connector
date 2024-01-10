using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Factory;

namespace SmartIOT.Connector.DependencyInjection;

public static class AspNetCoreExtensions
{
    public static IServiceCollection AddSmartIOTConnector(this IServiceCollection services, Action<SmartIotConnectorBuilder> configure)
    {
        var builder = new SmartIotConnectorBuilder();

        configure?.Invoke(builder);
        ArgumentNullException.ThrowIfNull(builder.Configuration);

        // add main stuffs
        services.AddSingleton<SmartIotConnectorBuilder>(builder);
        services.AddSingleton<SmartIotConnectorConfiguration>(builder.Configuration);
        services.AddSingleton<SmartIotConnector>(builder.Build);

        // expose more things on DI
        services.AddSingleton<IConnectorFactory>(_ => builder.ConnectorFactory);
        services.AddSingleton<IDeviceDriverFactory>(_ => builder.DeviceDriverFactory);
        services.AddSingleton<ISchedulerFactory>(_ => builder.SchedulerFactory);
        services.AddSingleton<ITimeService>(_ => builder.TimeService);

        // add hosted service
        services.AddSingleton<SmartIotConnectorHostedService>();
        services.AddHostedService(sp => sp.GetRequiredService<SmartIotConnectorHostedService>());

        return services;
    }

    public static IApplicationBuilder UseSmartIOTConnector(this IApplicationBuilder app, Action<SmartIotConnector> configure)
    {
        var connector = app.ApplicationServices.GetRequiredService<SmartIotConnector>();

        configure.Invoke(connector);

        return app;
    }
}
