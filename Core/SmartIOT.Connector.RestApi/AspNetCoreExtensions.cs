using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using SmartIOT.Connector.Core;
using SmartIOT.Connector.RestApi.Services;

namespace SmartIOT.Connector.RestApi
{
	public static class AspNetCoreExtensions
	{
		public static IServiceCollection AddSmartIotConnectorRestApi(this IServiceCollection services, IConfigurationPersister configurationPersister)
		{
			services.AddControllers();
			services.AddApiVersioning(config =>
			{
				config.DefaultApiVersion = new ApiVersion(1, 0);
				config.AssumeDefaultVersionWhenUnspecified = true;
				config.ReportApiVersions = true;
			});

			services.AddVersionedApiExplorer(setup =>
			{
				setup.GroupNameFormat = "'v'VVV";
				setup.SubstituteApiVersionInUrl = true;
			});

			services.ConfigureOptions<SwaggerVersioningOptions>();

			services.AddTransient<IConnectorService, ConnectorService>();
			services.AddTransient<IDeviceService, DeviceService>();
			services.AddTransient<IConfigurationService>(s => new ConfigurationService(s.GetRequiredService<SmartIotConnector>(), configurationPersister));

			return services;
		}
	}
}
