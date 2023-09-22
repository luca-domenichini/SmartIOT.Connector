using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Serilog;
using SmartIOT.Connector.RestApi;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace SmartIOT.Connector.ConsoleApp;

public class Program
{
    protected Program()
    {
    }

    public static void Main(string[] args)
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
        var version = fileVersionInfo.ProductVersion ?? "--Unknown";

        if (args.Length == 0)
            args = new[] { "smartiot-config.json" };

        string path = args[0];
        if (!File.Exists(path))
        {
            Console.WriteLine($"Configuration file {path} does not exists");
            return;
        }

        var configuration = JsonSerializer.Deserialize<AppConfiguration>(File.ReadAllText(path), new JsonSerializerOptions()
        {
            ReadCommentHandling = JsonCommentHandling.Skip
        });
        if (configuration == null)
        {
            Console.WriteLine($"Configuration not valid for file {path}");
            return;
        }

        SetupSerilog();

        var builder = WebApplication.CreateBuilder(args);

        // configure logging
        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(dispose: true);

        // Add SmartIOT.Connector services to the container.
        builder.Services.AddSmartIotConnectorRunner(configuration);

        // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // Add api controllers and versioning
        builder.Services.AddSmartIotConnectorRestApi(new ConfigurationPersister(configuration, path));

        builder.Services.AddRouting(options =>
        {
            options.LowercaseUrls = true;
            options.LowercaseQueryStrings = true;
        });

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            var provider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();

            foreach (var name in provider.ApiVersionDescriptions.Select(x => x.GroupName))
            {
                options.SwaggerEndpoint($"/swagger/{name}/swagger.json", $"SmartIOT.Connector API {name}");
            }
        });

        app.UseHttpsRedirection();

        app.UseAuthorization();

        app.MapControllers();

        // log something before the real start
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("{NewLine}{NewLine}  --> SmartIOT.Connector v{version}{NewLine}", Environment.NewLine, Environment.NewLine, version, Environment.NewLine);

        app.Run();
    }

    private static void SetupSerilog()
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddEnvironmentVariables()
            .Build();

        var conf = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.FromLogContext();

        Log.Logger = conf.CreateLogger();
    }
}