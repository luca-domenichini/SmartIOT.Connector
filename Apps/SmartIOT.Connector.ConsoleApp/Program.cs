using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace SmartIOT.Connector.ConsoleApp
{
	public class Program
	{
		public static void Main(string[] args)
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
			var version = fileVersionInfo.ProductVersion ?? "--Unknown";

			WriteInfo($"SmartIOT.Connector v{version}");

			if (args.Length == 0)
				args = new[] { "smartiot-config.json" };

			string path = args[0];
			if (!File.Exists(path))
			{
				WriteError($"Configuration file {path} does not exists");
				return;
			}

			WriteInfo($"Configuring SmartIotConnector from file {path}");

			var configuration = JsonSerializer.Deserialize<RunnerConfiguration>(File.ReadAllText(path), new JsonSerializerOptions()
			{
				ReadCommentHandling = JsonCommentHandling.Skip
			});
			if (configuration == null)
			{
				WriteError($"Configuration not valid");
				return;
			}

			var runner = new Runner(configuration
				, onExceptionDuringDiscovery: exceptions =>
				{
					WriteError($"Warning: error autodiscoverying dll: [{Environment.NewLine}{string.Join($"{Environment.NewLine}\t", exceptions.Select(x => x.Message))}{Environment.NewLine}]");
				});

			runner.RunAndWaitForShutdown(
				onStartingHandler: (s, e) => WriteInfo("SmartIOT.Connector starting..")
				, onStartedHandler: (s, e) => WriteInfo("SmartIOT.Connector started. Press Ctrl-C for graceful stop.")
				, onStoppingHandler: (s, e) => WriteInfo("SmartIOT.Connector stopping..")
				, onStoppedHandler: (s, e) => WriteInfo("SmartIOT.Connector stopped")
				, onExceptionHandler: (s, e) => WriteError($"Exception caught: {e.Exception.Message}", e.Exception)
				, onTagRead: (s, e) =>
				{
					if (e.TagScheduleEvent.Data != null)
					{
						// data event
						if (e.TagScheduleEvent.Data.Length > 0)
							WriteInfo($"{e.DeviceDriver.Name}: Device {e.TagScheduleEvent.Device.DeviceId}, Tag {e.TagScheduleEvent.Tag.TagId}: received data[{e.TagScheduleEvent.Data.Length}]");
					}
					else if (e.TagScheduleEvent.IsErrorNumberChanged)
					{
						// status changed
						WriteInfo($"{e.DeviceDriver.Name}: Device {e.TagScheduleEvent.Device.DeviceId}, Tag {e.TagScheduleEvent.Tag.TagId}: status changed {e.TagScheduleEvent.ErrorNumber} {e.TagScheduleEvent.Description}");
					}
				}
				, onTagWrite: (s, e) =>
				{

				}
				, onSchedulerRestarting: (s, e) => WriteInfo($"{e.DeviceDriver.Name}: Scheduler restarting")
				, onSchedulerRestarted: (s, e) =>
				{
					if (e.IsSuccess)
						WriteInfo($"{e.DeviceDriver.Name}: Scheduler restarted successfully");
					else
						WriteError($"{e.DeviceDriver.Name}: Error during scheduler restart: {e.ErrorDescription}");
				}
				, onConnectorStartedHandler: (s, e) =>
				{
					WriteInfo($"{e.Connector.GetType().Name}: {e.Info}");
				}
				, onConnectorStoppedHandler: (s, e) =>
				{
					WriteInfo($"{e.Connector.GetType().Name}: {e.Info}");
				}
				, onConnectorConnectedHandler: (s, e) =>
				{
					WriteInfo($"{e.Connector.GetType().Name}: {e.Info}");
				}
				, onConnectorConnectionFailedHandler: (s, e) =>
				{
					WriteError($"{e.Connector.GetType().Name}: {e.Info}", e.Exception);
				}
				, onConnectorDisconnectedHandler: (s, e) =>
				{
					WriteInfo($"{e.Connector.GetType().Name}: {e.Info}");
				}
				, onConnectorExceptionHandler: (s, e) =>
				{
					WriteError($"{e.Connector.GetType().Name}: Unexpected exception: {e.Exception.Message}", e.Exception);
				}
				);
		}

		public static void WriteInfo(string message)
		{
			Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [INFO] {message}");
		}
		public static void WriteError(string message, Exception? exception = null)
		{
			Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [ERROR] {message}");
			if (exception != null)
				Console.WriteLine(exception);
		}
	}
}



