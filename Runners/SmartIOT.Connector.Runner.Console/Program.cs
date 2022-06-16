using System.Reflection;
using System.Text.Json;

namespace SmartIOT.Connector.Runner.Console
{
	public class Program
	{
		public static void Main(string[] args)
		{
			WriteInfo($"SmartIotConnector v{Assembly.GetExecutingAssembly().GetName().Version}");

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
					WriteError($"Warning: error autodiscoverying dll: [\r\n{string.Join("\r\n\t", exceptions.Select(x => x.Message))}\r\n]");
				});

			runner.RunAndWaitForShutdown(
				onStartingHandler: (s, e) => WriteInfo("SmartIotConnector starting..")
				, onStartedHandler: (s, e) => WriteInfo("SmartIotConnector started. Press Ctrl-C for graceful stop.")
				, onStoppingHandler: (s, e) => WriteInfo("SmartIotConnector stopping..")
				, onStoppedHandler: (s, e) => WriteInfo("SmartIotConnector stopped")
				, onExceptionHandler: (s, e) => WriteError($"Exception caught: {e.Exception.Message}\r\n{e.Exception}")
				, onTagRead: (s, e) =>
				{
					if (e.TagScheduleEvent.Data != null)
					{
						// data event
						if (e.TagScheduleEvent.Data.Length > 0)
							WriteInfo($"Device {e.TagScheduleEvent.Device.DeviceId}, Tag {e.TagScheduleEvent.Tag.TagId}: received data[{e.TagScheduleEvent.Data.Length}]");
					}
					else if (e.TagScheduleEvent.ErrorNumber == 0)
					{
						// status OK event
						WriteInfo($"Device {e.TagScheduleEvent.Device.DeviceId}, Tag {e.TagScheduleEvent.Tag.TagId}: received status {e.TagScheduleEvent.ErrorNumber} {e.TagScheduleEvent.Description}");
					}
					else
					{
						// statu KO event
						WriteInfo($"Device {e.TagScheduleEvent.Device.DeviceId}, Tag {e.TagScheduleEvent.Tag.TagId}: received status {e.TagScheduleEvent.ErrorNumber} {e.TagScheduleEvent.Description}");
					}
				}
				, onTagWrite: (s, e) =>
				{

				}
				, onSchedulerRestarting: (s, e) => WriteInfo("Scheduler restarting")
				, onSchedulerRestarted: (s, e) =>
				{
					if (e.IsSuccess)
						WriteInfo($"Scheduler restarted successfully");
					else
						WriteError($"Error during scheduler restart: {e.ErrorDescription}");
				}
				, onConnectorConnectedHandler: (s, e) =>
				{
					WriteInfo($"{e.Info}");
				}
				, onConnectorDisconnectedHandler: (s, e) =>
				{
					WriteInfo($"{e.Info}");
				});
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



