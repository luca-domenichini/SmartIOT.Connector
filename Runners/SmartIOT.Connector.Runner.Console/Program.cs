using System.Diagnostics;
using System.Reflection;
using System.Text.Json;

namespace SmartIOT.Connector.Runner.Console
{
	public class Program
	{
		public static void Main(string[] args)
		{
			Assembly assembly = Assembly.GetExecutingAssembly();
			FileVersionInfo fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
			var version = fileVersionInfo.ProductVersion ?? "Unkwown";

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
				onStartingHandler: (s, e) => WriteInfo("SmartIotConnector starting..")
				, onStartedHandler: (s, e) => WriteInfo("SmartIotConnector started. Press Ctrl-C for graceful stop.")
				, onStoppingHandler: (s, e) => WriteInfo("SmartIotConnector stopping..")
				, onStoppedHandler: (s, e) => WriteInfo("SmartIotConnector stopped")
				, onExceptionHandler: (s, e) => WriteError($"Exception caught: {e.Exception.Message}{Environment.NewLine}{e.Exception}")
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
				, onConnectorDisconnectedHandler: (s, e) =>
				{
					WriteInfo($"{e.Connector.GetType().Name}: {e.Info}");
				}
				, onConnectorExceptionHandler: (s, e) =>
				{
					WriteError($"{e.Connector.GetType().Name}: Unexpected exception: {e.Exception.Message}{Environment.NewLine}{e.Exception}");
				}
				);
		}

		public static void WriteInfo(string message)
		{
			System.Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [INFO] {message}");
		}
		public static void WriteError(string message)
		{
			System.Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [ERROR] {message}");
		}
	}
}



