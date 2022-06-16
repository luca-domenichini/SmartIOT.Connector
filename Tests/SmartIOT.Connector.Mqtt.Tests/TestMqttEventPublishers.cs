using Moq;
using MQTTnet;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Server;
using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Connector;
using SmartIOT.Connector.Core.Events;
using SmartIOT.Connector.Core.Scheduler;
using SmartIOT.Connector.Core.Tests;
using SmartIOT.Connector.Device.Mocks;
using SmartIOT.Connector.Messages;
using SmartIOT.Connector.Messages.Serializers;
using SmartIOT.Connector.Mqtt.Client;
using SmartIOT.Connector.Mqtt.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

namespace SmartIOT.Connector.Mqtt.Tests
{
	public class TestMqttEventPublishers : SmartIOTBaseTests
	{
		[Fact]
		public void Test_MqttConnector_with_mock_publisher()
		{
			var publisher = new MockEventPublisher();
			var connector = new MqttConnector(new ConnectorOptions()
			{
				IsPublishWriteEvents = true
			}, publisher);

			SmartIotConnector module = SetupSmartIotConnector(SetupConfiguration(new DeviceConfiguration("mock://mock", "1", true, "MockDevice", new List<TagConfiguration>()
			{
				new TagConfiguration("DB20", TagType.READ, 10, 100, 1),
				new TagConfiguration("DB22", TagType.WRITE, 10, 100, 1)
			})), connector);

			MockDeviceDriver driver = (MockDeviceDriver)module.Schedulers[0].DeviceDriver;
			driver.SetupReadTagAsRandomData(15, 10);

			module.Start();
			try
			{
				Thread.Sleep(1000);

				// richiedo scrittura dati
				publisher.RequestTagWrite(new TagWriteRequestCommand("1", "DB22", 20, new byte[] { 1, 2, 3, 4, 5 }));
				Thread.Sleep(1000);

				module.Stop();

				publisher.Verify(x => x.Start(It.IsAny<MqttConnector>(), It.IsAny<ISmartIOTConnectorInterface>()), Times.Once);
				publisher.Verify(x => x.Stop(), Times.Once);
				publisher.Verify(x => x.PublishDeviceStatusEvent(It.IsAny<DeviceStatusEvent>()), Times.AtLeastOnce);
				publisher.Verify(x => x.PublishTagScheduleEvent(It.Is<TagScheduleEvent>(x => x.Tag.TagId == "DB20")), Times.AtLeastOnce);
				publisher.Verify(x => x.PublishTagScheduleEvent(It.Is<TagScheduleEvent>(x => x.Tag.TagId == "DB22")), Times.AtLeastOnce);
				publisher.Verify(x => x.PublishTagScheduleEvent(It.Is<TagScheduleEvent>(x => x.Tag.TagId == "DB22" && x.StartOffset == 20 && x.Data!.Length == 5)), Times.Once);
			}
			finally
			{
				module.Stop();
			}
		}

		[Theory]
		[InlineData(true, "json")]
		[InlineData(true, "protobuf")]
		[InlineData(false, "json")]
		[InlineData(false, "protobuf")]
		public void Test_MqttConnector_with_real_mqttServer_publisher(bool isPublishPartialReads, string serializerType)
		{
			IList<TagEvent> tagEvents = new List<TagEvent>();
			IList<DeviceEvent> deviceStatusEvents = new List<DeviceEvent>();
			IList<MqttApplicationMessage> otherMessages = new List<MqttApplicationMessage>();

			tagEvents.Clear();
			deviceStatusEvents.Clear();
			otherMessages.Clear();

			ISingleMessageSerializer serializer;
			if (serializerType == "json")
				serializer = new JsonSingleMessageSerializer();
			else if (serializerType == "protobuf")
				serializer = new ProtobufSingleMessageSerializer();
			else
				throw new InvalidOperationException("serializer not valid");

			var client = new MqttFactory().CreateMqttClient();

			var mqttClientOptions = new MqttClientOptionsBuilder()
				.WithTcpServer("localhost", 1883)
				.WithClientId("TestClient")
				.Build();

			client.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(eventArgs =>
			{
				lock (this) // lock to do asserts safely
				{
					if (eventArgs.ApplicationMessage.Topic.StartsWith("tagRead/"))
						tagEvents.Add(serializer.DeserializeMessage<TagEvent>(eventArgs.ApplicationMessage.Payload)!);
					else if (eventArgs.ApplicationMessage.Topic.StartsWith("deviceStatus/"))
						deviceStatusEvents.Add(serializer.DeserializeMessage<DeviceEvent>(eventArgs.ApplicationMessage.Payload)!);
					else
						otherMessages.Add(eventArgs.ApplicationMessage);
				}
			});

			var publisher = new MqttServerEventPublisher(serializer, new MqttServerEventPublisherOptions(Guid.NewGuid().ToString("N"), 1883, "exceptions", "deviceStatus/device${DeviceId}", "tagRead/device${DeviceId}/tag${TagId}", "tagWrite", isPublishPartialReads));
			var connector = new MqttConnector(new ConnectorOptions(), publisher);

			SmartIotConnector module = SetupSmartIotConnector(
				SetupConfiguration(
					new DeviceConfiguration("mock://mock", "1", true, "MockDevice"
						, new List<TagConfiguration>()
						{
							new TagConfiguration("DB20", TagType.READ, 10, 100, 1),
							new TagConfiguration("DB22", TagType.WRITE, 10, 100, 1),
						}
					)
				)
				, connector);

			MockDeviceDriver driver = (MockDeviceDriver)module.Schedulers[0].DeviceDriver;
			driver.SetupReadTagAsRandomData(15, 10);

			module.Start();
			try
			{
				Thread.Sleep(1000);

				client.ConnectAsync(mqttClientOptions, CancellationToken.None).Wait();

				client.SubscribeAsync(new MQTTnet.Client.Subscribing.MqttClientSubscribeOptions
				{
					TopicFilters = new List<MqttTopicFilter>
					{
						new MqttTopicFilter()
						{
							QualityOfServiceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce,
							Topic = "exceptions",
						},
						new MqttTopicFilter()
						{
							QualityOfServiceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce,
							Topic = "deviceStatus/#",
						},
						new MqttTopicFilter()
						{
							QualityOfServiceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce,
							Topic = "tagRead/#",
						},
					}
				}, CancellationToken.None).Wait();

				Thread.Sleep(1000);

				lock (this)
				{
					Assert.Equal(0, otherMessages.Count);
					Assert.True(tagEvents.Count > 0);

					if (isPublishPartialReads)
					{
						IEnumerable<TagEvent> initEvents = tagEvents.Where(x => x.DeviceId == "1" && (x.TagId == "DB20" || x.TagId == "DB22") && x.StartOffset == 10 && x.Data != null && x.Data.Length == 100 && x.IsInitializationEvent);
						Assert.Equal(2, initEvents.Count()); // 2 eventi di lettura completa (non necessariamente all'inizio dello stream)
						Assert.All(tagEvents.Except(initEvents), x => Assert.True(x.DeviceId == "1" && x.TagId == "DB20" && x.StartOffset == 15 && x.Data != null && x.Data.Length == 10)); // N eventi di lettura parziale
					}
					else
					{
						var tag22Events = tagEvents.Where(x => x.TagId == "DB22");
						Assert.All(tagEvents.Except(tag22Events), x => Assert.True(x.DeviceId == "1" && x.TagId == "DB20" && x.StartOffset == 10 && x.Data != null && x.Data.Length == 100)); // N eventi di lettura completa
					}

					Assert.True(deviceStatusEvents.Count > 0);
					Assert.True(deviceStatusEvents.All(x => x.DeviceId == "1" && x.DeviceStatus == DeviceStatus.OK && x.ErrorNumber == 0 && string.IsNullOrEmpty(x.Description)));
				}

				// richiedo scrittura dati inviando un messaggio al server
				var wasWritten = new AutoResetEvent(false);
				module.TagWriteEvent += (sender, e) =>
				{
					if (e.TagScheduleEvent.Tag.TagId == "DB22")
						wasWritten.Set();
				};

				client.PublishAsync(new MqttApplicationMessageBuilder()
					.WithTopic("tagWrite")
					.WithPayload(serializer.SerializeMessage(new TagWriteRequestCommand("1", "DB22", 20, new byte[] { 1, 2, 3, 4, 5 })))
					.WithAtLeastOnceQoS()
					.Build(), CancellationToken.None).Wait();

				Assert.True(wasWritten.WaitOne(TimeSpan.FromSeconds(2)));
			}
			finally
			{
				module.Stop();

				client.DisconnectAsync(new MQTTnet.Client.Disconnecting.MqttClientDisconnectOptions()
				{
					ReasonCode = MQTTnet.Client.Disconnecting.MqttClientDisconnectReason.NormalDisconnection,
					ReasonString = string.Empty
				}, CancellationToken.None).Wait();
			}
		}

		[Theory]
		[InlineData("json")]
		[InlineData("protobuf")]
		public void Test_scheduler_and_MqttConnector_with_real_mqttServer_publisher(string serializerType)
		{
			IList<TagEvent> tagEvents = new List<TagEvent>();
			IList<DeviceEvent> deviceStatusEvents = new List<DeviceEvent>();
			IList<MqttApplicationMessage> otherMessages = new List<MqttApplicationMessage>();

			var tagEvent = new ManualResetEventSlim();
			var deviceStatusEvent = new ManualResetEventSlim();
			var otherMessagesEvent = new ManualResetEventSlim();
			var connectedEvent = new ManualResetEventSlim();

			tagEvents.Clear();
			deviceStatusEvents.Clear();
			otherMessages.Clear();

			ISingleMessageSerializer serializer;
			if (serializerType == "json")
				serializer = new JsonSingleMessageSerializer();
			else if (serializerType == "protobuf")
				serializer = new ProtobufSingleMessageSerializer();
			else
				throw new InvalidOperationException("serializer not valid");


			var publisher = new MqttServerEventPublisher(serializer, new MqttServerEventPublisherOptions(Guid.NewGuid().ToString("N"), 1883, "exceptions", "deviceStatus/device${DeviceId}", "tagRead/device${DeviceId}/tag${TagId}", "tagWrite", true));
			var connector = new MqttConnector(new ConnectorOptions(), publisher);

			DeviceConfiguration deviceConfiguration = new DeviceConfiguration("mock://mock", "1", true, "MockDevice"
				, new List<TagConfiguration>()
				{
					new TagConfiguration("DB20", TagType.READ, 10, 100, 1)
				}
			);
			var configuration = SetupConfiguration(deviceConfiguration);

			MockDeviceDriver driver = new MockDeviceDriver(new Core.Model.Device(deviceConfiguration));
			driver.SetupReadTagAsRandomData(15, 10);

			var device = driver.GetDevices(true)[0];
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

			var client = new MqttFactory().CreateMqttClient();

			try
			{
				connector.Start(connectorInterface.Object);

				var mqttClientOptions = new MqttClientOptionsBuilder()
					.WithTcpServer("localhost", 1883)
					.WithClientId("TestClient")
					.Build();

				client.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(eventArgs =>
				{
					if (eventArgs.ApplicationMessage.Topic.StartsWith("tagRead/"))
					{
						tagEvents.Add(serializer.DeserializeMessage<TagEvent>(eventArgs.ApplicationMessage.Payload)!);
						tagEvent.Set();
					}
					else if (eventArgs.ApplicationMessage.Topic.StartsWith("deviceStatus/"))
					{
						deviceStatusEvents.Add(serializer.DeserializeMessage<DeviceEvent>(eventArgs.ApplicationMessage.Payload)!);
						deviceStatusEvent.Set();
					}
					else
					{
						otherMessages.Add(eventArgs.ApplicationMessage);
						otherMessagesEvent.Set();
					}
				});

				client.ConnectAsync(mqttClientOptions, CancellationToken.None).Wait();

				Thread.Sleep(100);


				Assert.True(connectedEvent.Wait(1000));
				Assert.Equal(WaitHandle.WaitTimeout, WaitHandle.WaitAny(new[] { tagEvent.WaitHandle, deviceStatusEvent.WaitHandle }, 1000));
				Assert.Empty(tagEvents);
				Assert.Empty(deviceStatusEvents);
				Assert.Empty(otherMessages);

				tagEvent.Reset();
				deviceStatusEvent.Reset();
				otherMessagesEvent.Reset();


				client.SubscribeAsync(new MQTTnet.Client.Subscribing.MqttClientSubscribeOptions
				{
					TopicFilters = new List<MqttTopicFilter>
					{
						new MqttTopicFilter()
						{
							QualityOfServiceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce,
							Topic = "exceptions",
						},
						new MqttTopicFilter()
						{
							QualityOfServiceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce,
							Topic = "deviceStatus/#",
						},
						new MqttTopicFilter()
						{
							QualityOfServiceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce,
							Topic = "tagRead/#",
						},
					}
				}, CancellationToken.None).Wait();

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

				client.DisconnectAsync(new MQTTnet.Client.Disconnecting.MqttClientDisconnectOptions()
				{
					ReasonCode = MQTTnet.Client.Disconnecting.MqttClientDisconnectReason.NormalDisconnection,
					ReasonString = string.Empty
				}, CancellationToken.None).Wait();
			}
		}

		[Theory]
		[InlineData("json")]
		[InlineData("protobuf")]
		public void Test_MqttConnector_with_real_mqttClient_publisher(string serializerType)
		{
			IList<TagEvent> tagEvents = new List<TagEvent>();
			IList<DeviceEvent> deviceStatusEvents = new List<DeviceEvent>();
			IList<MqttApplicationMessage> otherMessages = new List<MqttApplicationMessage>();

			var tagEvent = new ManualResetEventSlim();
			var deviceStatusEvent = new ManualResetEventSlim();

			tagEvents.Clear();
			deviceStatusEvents.Clear();
			otherMessages.Clear();

			var server = new MqttFactory().CreateMqttServer();

			var mqttServerOptions = new MqttServerOptionsBuilder()
				.WithDefaultEndpoint()
				.WithDefaultEndpointPort(1883)
				.WithClientId("TestServer")
				.Build();

			ISingleMessageSerializer serializer;
			if (serializerType == "json")
				serializer = new JsonSingleMessageSerializer();
			else if (serializerType == "protobuf")
				serializer = new ProtobufSingleMessageSerializer();
			else
				throw new InvalidOperationException("serializer not valid");


			server.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(eventArgs =>
			{
				lock (this) // lock to do asserts safely
				{
					if (eventArgs.ApplicationMessage.Topic.StartsWith("tagRead/"))
					{
						tagEvents.Add(serializer.DeserializeMessage<TagEvent>(eventArgs.ApplicationMessage.Payload)!);
						tagEvent.Set();
					}
					else if (eventArgs.ApplicationMessage.Topic.StartsWith("deviceStatus/"))
					{
						deviceStatusEvents.Add(serializer.DeserializeMessage<DeviceEvent>(eventArgs.ApplicationMessage.Payload)!);
						deviceStatusEvent.Set();
					}
					else
						otherMessages.Add(eventArgs.ApplicationMessage);
				}
			});

			var publisher = new MqttClientEventPublisher(serializer, new MqttClientEventPublisherOptions(Guid.NewGuid().ToString("N"), "localhost", 1883, "exceptions", "deviceStatus/device${DeviceId}", "tagRead/device${DeviceId}/tag${TagId}", "tagWrite", "", ""));
			var connector = new MqttConnector(new ConnectorOptions(), publisher);

			SmartIotConnector module = SetupSmartIotConnector(
				SetupConfiguration(
					new DeviceConfiguration("mock://mock", "1", true, "MockDevice"
						, new List<TagConfiguration>()
						{
							new TagConfiguration("DB20", TagType.READ, 10, 100, 1),
							new TagConfiguration("DB22", TagType.WRITE, 10, 100, 1)
						}
					)
				)
				, connector);

			MockDeviceDriver driver = (MockDeviceDriver)module.Schedulers[0].DeviceDriver;
			driver.SetupReadTagAsRandomData(15, 10);

			try
			{
				module.Start();
				Thread.Sleep(1000);

				// il server parte dopo aver fatto partire il client: in questo modo sto testando anche il fatto che il client si riconnetta in automatico
				server.StartAsync(mqttServerOptions).Wait();
				server.SubscribeAsync("TestServer", "exceptions");
				server.SubscribeAsync("TestServer", "deviceStatus/#");
				server.SubscribeAsync("TestServer", "tagRead/#");

				if (!tagEvent.Wait(2000))
					throw new Exception("No tagReadEvent has been received");
				else if (!deviceStatusEvent.Wait(2000))
					throw new Exception("No deviceStatusEvent has been received");
				else
					Thread.Sleep(2000); // wait 2 sec for otherMessages

				lock (this)
				{
					Assert.Equal(0, otherMessages.Count);
					Assert.True(tagEvents.Count > 0);
					Assert.All(tagEvents, x => Assert.True(x.DeviceId == "1" && (x.TagId == "DB20" || x.TagId == "DB22") && x.StartOffset == 10 && x.Data != null && x.Data.Length == 100)); // N eventi di lettura completa (MqttEventPublisher publica sempre tutto il tag)
					Assert.True(deviceStatusEvents.Count > 0);
					Assert.True(deviceStatusEvents.All(x => x.DeviceId == "1" && x.DeviceStatus == DeviceStatus.OK && x.ErrorNumber == 0 && string.IsNullOrEmpty(x.Description)));
				}

				// test scrittura dati
				var wasWritten = new AutoResetEvent(false);
				module.TagWriteEvent += (sender, e) =>
				{
					if (e.TagScheduleEvent.Tag.TagId == "DB22")
						wasWritten.Set();
				};

				server.PublishAsync(b => b
					.WithAtLeastOnceQoS()
					.WithTopic("tagWrite")
					.WithPayload(serializer.SerializeMessage(new TagWriteRequestCommand("1", "DB22", 20, new byte[] { 1, 2, 3, 4, 5 })))
				).Wait();

				Assert.True(wasWritten.WaitOne(TimeSpan.FromSeconds(2)));
			}
			finally
			{
				module.Stop();
				server.StopAsync().Wait();
			}
		}

	}
}