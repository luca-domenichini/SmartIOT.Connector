using Moq;
using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Events;
using SmartIOT.Connector.Core.Scheduler;
using SmartIOT.Connector.Core.Tests;
using SmartIOT.Connector.Mocks;
using SmartIOT.Connector.Messages;
using SmartIOT.Connector.Messages.Serializers;
using SmartIOT.Connector.Tcp.Client;
using SmartIOT.Connector.Tcp.Server;
using System.Net;
using System.Net.Sockets;

namespace SmartIOT.Connector.Tcp.Tests
{
	public class TcpConnectorTests : SmartIOTBaseTests
	{
		[Theory]
		[InlineData("json", 1884)]
		[InlineData("protobuf", 1885)]
		public void Test_scheduler_and_TcpClientConnector(string serializerType, int port)
		{
			IList<TagEvent> tagEvents = new List<TagEvent>();
			IList<DeviceEvent> deviceStatusEvents = new List<DeviceEvent>();
			IList<object> otherMessages = new List<object>();

			var tagEvent = new ManualResetEventSlim();
			var deviceStatusEvent = new ManualResetEventSlim();
			var otherMessagesEvent = new ManualResetEventSlim();
			var connectedEvent = new ManualResetEventSlim();

			tagEvents.Clear();
			deviceStatusEvents.Clear();
			otherMessages.Clear();

			IStreamMessageSerializer serializer;
			if (serializerType == "json")
				serializer = new JsonStreamMessageSerializer();
			else if (serializerType == "protobuf")
				serializer = new ProtobufStreamMessageSerializer();
			else
				throw new InvalidOperationException("serializer not valid");


			var connector = new TcpClientConnector(new TcpClientConnectorOptions("tcpClient://", false, "localhost", port, TimeSpan.FromSeconds(5), serializer, TimeSpan.Zero));

			DeviceConfiguration deviceConfiguration = new DeviceConfiguration("mock://mock", "1", true, "MockDevice"
				, new List<TagConfiguration>()
				{
					new TagConfiguration("DB20", TagType.READ, 10, 100, 1)
				}
			);
			var configuration = SetupConfiguration(deviceConfiguration);

			MockDeviceDriver driver = new MockDeviceDriver(new Core.Model.Device(deviceConfiguration));
			driver.SetupReadTagAsRandomData(15, 10);

			var device = driver.Device;
			var tag = device.Tags[0];

			TagSchedulerEngine engine = new TagSchedulerEngine(driver, new TimeService(), configuration.SchedulerConfiguration);
			engine.TagReadEvent += connector.OnTagReadEvent;
			engine.DeviceStatusEvent += connector.OnDeviceStatusEvent;
			engine.TagWriteEvent += connector.OnTagWriteEvent;
			engine.ExceptionHandler += connector.OnException;


			DeviceStatusEvent? lastDeviceStatusEvent = null;
			TagScheduleEvent? lastTagScheduleEvent = null;

			engine.DeviceStatusEvent += (s, e) =>
			{
				lastDeviceStatusEvent = e.DeviceStatusEvent;
			};
			engine.TagReadEvent += (s, e) =>
			{
				lastTagScheduleEvent = e.TagScheduleEvent;
			};

			Assert.True(engine.IsRestartNeeded());

			engine.RestartDriver();

			Assert.NotNull(lastDeviceStatusEvent);
			Assert.NotNull(lastTagScheduleEvent);
			Assert.False(engine.IsRestartNeeded());

			lastTagScheduleEvent = null;
			engine.ScheduleNextTag(false);

			Assert.NotNull(lastTagScheduleEvent);
			Assert.Equal(0, otherMessages.Count);
			Assert.Equal(0, tagEvents.Count);
			Assert.Equal(0, deviceStatusEvents.Count);

			var connectorInterface = new Mock<ISmartIOTConnectorInterface>();

			connectorInterface.Setup(x => x.RunInitializationAction(It.IsAny<Action<IList<DeviceStatusEvent>, IList<TagScheduleEvent>>>()))
				.Callback((Action<IList<DeviceStatusEvent>, IList<TagScheduleEvent>> initAction) =>
				{
					var listDeviceEvents = new List<DeviceStatusEvent>();
					var listTagEvents = new List<TagScheduleEvent>();

					if (lastDeviceStatusEvent != null)
						listDeviceEvents.Add(lastDeviceStatusEvent);

					listTagEvents.Add(TagScheduleEvent.BuildTagData(device, tag, false));

					initAction.Invoke(listDeviceEvents, listTagEvents);
				});

			connectorInterface.Setup(x => x.OnConnectorConnected(It.IsAny<ConnectorConnectedEventArgs>()))
				.Callback((ConnectorConnectedEventArgs e) =>
				{
					connectedEvent.Set();
				});

			var server = new TcpListener(IPAddress.Loopback, port);

			Stream? stream = null;
			CancellationTokenSource? token = null;

			try
			{
				connector.Start(connectorInterface.Object);
				server.Start();

				TcpClient clientHandler = server.AcceptTcpClient();

				stream = clientHandler.GetStream();

				token = new CancellationTokenSource();

				var task = Task.Run(() =>
				{
					while (!token.IsCancellationRequested)
					{
						var message = serializer.DeserializeMessage(stream);
						if (message is TagEvent t)
						{
							tagEvents.Add(t);
							tagEvent.Set();
						}
						else if (message is DeviceEvent d)
						{
							deviceStatusEvents.Add(d);
							deviceStatusEvent.Set();
						}
						else if (message != null)
						{
							otherMessages.Add(message);
							otherMessagesEvent.Set();
						}
					}
				});

				Assert.True(WaitHandle.WaitAll(new[] { tagEvent.WaitHandle, deviceStatusEvent.WaitHandle }, 2000));

				Assert.Equal(0, otherMessages.Count);
				Assert.Equal(1, tagEvents.Count); // ho ricevuto un tagEvent
				Assert.True(tagEvents.All(x => x.DeviceId == "1" && x.TagId == "DB20" && x.StartOffset == 10 && x.Data != null && x.Data.Length == 100));
				Assert.Single(deviceStatusEvents);
				Assert.True(deviceStatusEvents.All(x => x.DeviceId == "1" && x.DeviceStatus == DeviceStatus.OK && x.ErrorNumber == 0 && string.IsNullOrEmpty(x.Description)));

				engine.ScheduleNextTag(false);

				tagEvent.Reset();

				Assert.True(tagEvent.Wait(1000));

				Assert.Equal(0, otherMessages.Count);
				Assert.Equal(2, tagEvents.Count); // ho ricevuto un altro tagEvent
				Assert.True(tagEvents.Take(1).All(x => x.DeviceId == "1" && x.TagId == "DB20" && x.StartOffset == 10 && x.Data != null && x.Data.Length == 100));
				Assert.True(tagEvents.Skip(1).All(x => x.DeviceId == "1" && x.TagId == "DB20" && x.StartOffset == 15 && x.Data != null && x.Data.Length == 10));
				Assert.Single(deviceStatusEvents);
				Assert.True(deviceStatusEvents.All(x => x.DeviceId == "1" && x.DeviceStatus == DeviceStatus.OK && x.ErrorNumber == 0 && string.IsNullOrEmpty(x.Description)));
			}
			finally
			{
				connector.Stop();

				token?.Cancel();
				stream?.Close();
				server.Stop();
			}
		}
		
		[Theory]
		[InlineData("json", 1984)]
		[InlineData("protobuf", 1985)]
		public void Test_scheduler_and_TcpServerConnector(string serializerType, int port)
		{
			IList<TagEvent> tagEvents = new List<TagEvent>();
			IList<DeviceEvent> deviceStatusEvents = new List<DeviceEvent>();
			IList<object> otherMessages = new List<object>();

			var tagEvent = new ManualResetEventSlim();
			var deviceStatusEvent = new ManualResetEventSlim();
			var otherMessagesEvent = new ManualResetEventSlim();
			var connectedEvent = new ManualResetEventSlim();

			tagEvents.Clear();
			deviceStatusEvents.Clear();
			otherMessages.Clear();

			IStreamMessageSerializer serializer;
			if (serializerType == "json")
				serializer = new JsonStreamMessageSerializer();
			else if (serializerType == "protobuf")
				serializer = new ProtobufStreamMessageSerializer();
			else
				throw new InvalidOperationException("serializer not valid");


			var connector = new TcpServerConnector(new TcpServerConnectorOptions("tcpServer://", false, port, serializer, TimeSpan.Zero));

			DeviceConfiguration deviceConfiguration = new DeviceConfiguration("mock://mock", "1", true, "MockDevice"
				, new List<TagConfiguration>()
				{
					new TagConfiguration("DB20", TagType.READ, 10, 100, 1)
				}
			);
			var configuration = SetupConfiguration(deviceConfiguration);

			MockDeviceDriver driver = new MockDeviceDriver(new Core.Model.Device(deviceConfiguration));
			driver.SetupReadTagAsRandomData(15, 10);

			var device = driver.Device;
			var tag = device.Tags[0];

			TagSchedulerEngine engine = new TagSchedulerEngine(driver, new TimeService(), configuration.SchedulerConfiguration);
			engine.TagReadEvent += connector.OnTagReadEvent;
			engine.DeviceStatusEvent += connector.OnDeviceStatusEvent;
			engine.TagWriteEvent += connector.OnTagWriteEvent;
			engine.ExceptionHandler += connector.OnException;


			DeviceStatusEvent? lastDeviceStatusEvent = null;
			TagScheduleEvent? lastTagScheduleEvent = null;

			engine.DeviceStatusEvent += (s, e) =>
			{
				lastDeviceStatusEvent = e.DeviceStatusEvent;
			};
			engine.TagReadEvent += (s, e) =>
			{
				lastTagScheduleEvent = e.TagScheduleEvent;
			};

			Assert.True(engine.IsRestartNeeded());

			engine.RestartDriver();

			Assert.NotNull(lastDeviceStatusEvent);
			Assert.NotNull(lastTagScheduleEvent);
			Assert.False(engine.IsRestartNeeded());

			lastTagScheduleEvent = null;
			engine.ScheduleNextTag(false);

			Assert.NotNull(lastTagScheduleEvent);
			Assert.Equal(0, otherMessages.Count);
			Assert.Equal(0, tagEvents.Count);
			Assert.Equal(0, deviceStatusEvents.Count);

			var connectorInterface = new Mock<ISmartIOTConnectorInterface>();

			connectorInterface.Setup(x => x.RunInitializationAction(It.IsAny<Action<IList<DeviceStatusEvent>, IList<TagScheduleEvent>>>()))
				.Callback((Action<IList<DeviceStatusEvent>, IList<TagScheduleEvent>> initAction) =>
				{
					var listDeviceEvents = new List<DeviceStatusEvent>();
					var listTagEvents = new List<TagScheduleEvent>();

					if (lastDeviceStatusEvent != null)
						listDeviceEvents.Add(lastDeviceStatusEvent);

					listTagEvents.Add(TagScheduleEvent.BuildTagData(device, tag, false));

					initAction.Invoke(listDeviceEvents, listTagEvents);
				});

			connectorInterface.Setup(x => x.OnConnectorConnected(It.IsAny<ConnectorConnectedEventArgs>()))
				.Callback((ConnectorConnectedEventArgs e) =>
				{
					connectedEvent.Set();
				});

			var client = new TcpClient();

			Stream? stream = null;
			CancellationTokenSource? token = null;

			try
			{
				connector.Start(connectorInterface.Object);
				client.Connect("localhost", port);

				stream = client.GetStream();

				token = new CancellationTokenSource();

				var task = Task.Run(() =>
				{
					while (!token.IsCancellationRequested)
					{
						var message = serializer.DeserializeMessage(stream);
						if (message is TagEvent t)
						{
							tagEvents.Add(t);
							tagEvent.Set();
						}
						else if (message is DeviceEvent d)
						{
							deviceStatusEvents.Add(d);
							deviceStatusEvent.Set();
						}
						else if (message != null)
						{
							otherMessages.Add(message);
							otherMessagesEvent.Set();
						}
					}
				});

				Assert.True(WaitHandle.WaitAll(new[] { tagEvent.WaitHandle, deviceStatusEvent.WaitHandle }, 2000));

				Assert.Equal(0, otherMessages.Count);
				Assert.Equal(1, tagEvents.Count); // ho ricevuto un tagEvent
				Assert.True(tagEvents.All(x => x.DeviceId == "1" && x.TagId == "DB20" && x.StartOffset == 10 && x.Data != null && x.Data.Length == 100));
				Assert.Single(deviceStatusEvents);
				Assert.True(deviceStatusEvents.All(x => x.DeviceId == "1" && x.DeviceStatus == DeviceStatus.OK && x.ErrorNumber == 0 && string.IsNullOrEmpty(x.Description)));

				engine.ScheduleNextTag(false);

				tagEvent.Reset();

				Assert.True(tagEvent.Wait(1000));

				Assert.Equal(0, otherMessages.Count);
				Assert.Equal(2, tagEvents.Count); // ho ricevuto un altro tagEvent
				Assert.True(tagEvents.Take(1).All(x => x.DeviceId == "1" && x.TagId == "DB20" && x.StartOffset == 10 && x.Data != null && x.Data.Length == 100));
				Assert.True(tagEvents.Skip(1).All(x => x.DeviceId == "1" && x.TagId == "DB20" && x.StartOffset == 15 && x.Data != null && x.Data.Length == 10));
				Assert.Single(deviceStatusEvents);
				Assert.True(deviceStatusEvents.All(x => x.DeviceId == "1" && x.DeviceStatus == DeviceStatus.OK && x.ErrorNumber == 0 && string.IsNullOrEmpty(x.Description)));
			}
			finally
			{
				connector.Stop();

				token?.Cancel();
				stream?.Close();
				client.Close();
			}
		}
	}
}