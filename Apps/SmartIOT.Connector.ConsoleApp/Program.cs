using Asp.Versioning.ApiExplorer;
using Serilog;
using SmartIOT.Connector.RestApi;
using System.Diagnostics;
using System.Text.Json;

namespace SmartIOT.Connector.ConsoleApp;

public class Program
{
    protected Program()
    {
    }

    public static void Main(string[] args)
    {
        FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(Process.GetCurrentProcess().MainModule?.FileName!);
        var version = fileVersionInfo?.ProductVersion ?? "--Unknown";

        Environment.CurrentDirectory = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule?.FileName!)!;

        if (args.Length == 0)
            args = ["smartiot-config.json"];

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

        // setup winservice
        builder.Services.AddWindowsService(o =>
        {
            o.ServiceName = typeof(Program).Assembly.GetName().Name!;
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