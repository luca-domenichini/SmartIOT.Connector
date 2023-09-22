using Moq;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Packets;
using MQTTnet.Server;
using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Events;
using SmartIOT.Connector.Core.Scheduler;
using SmartIOT.Connector.Core.Tests;
using SmartIOT.Connector.Messages;
using SmartIOT.Connector.Messages.Serializers;
using SmartIOT.Connector.Mocks;
using SmartIOT.Connector.Mqtt.Client;
using SmartIOT.Connector.Mqtt.Server;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SmartIOT.Connector.Mqtt.Tests;

public class MqttConnectorTests : SmartIOTBaseTests
{
    [Theory]
    [InlineData(true, "json")]
    [InlineData(true, "protobuf")]
    [InlineData(false, "json")]
    [InlineData(false, "protobuf")]
    public async Task Test_MqttServerConnector(bool isPublishPartialReads, string serializerType)
    {
        IList<TagEvent> tagEvents = new List<TagEvent>();
        IList<DeviceEvent> deviceStatusEvents = new List<DeviceEvent>();
        IList<MqttApplicationMessage> otherMessages = new List<MqttApplicationMessage>();

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

        client.ApplicationMessageReceivedAsync += eventArgs =>
        {
            lock (client) // lock to do asserts safely
            {
                if (eventArgs.ApplicationMessage.Topic.StartsWith("tagRead/"))
                    tagEvents.Add(serializer.DeserializeMessage<TagEvent>(eventArgs.ApplicationMessage.PayloadSegment.Array!)!);
                else if (eventArgs.ApplicationMessage.Topic.StartsWith("deviceStatus/"))
                    deviceStatusEvents.Add(serializer.DeserializeMessage<DeviceEvent>(eventArgs.ApplicationMessage.PayloadSegment.Array!)!);
                else
                    otherMessages.Add(eventArgs.ApplicationMessage);
            }

            return Task.CompletedTask;
        };

        var connector = new MqttServerConnector(new MqttServerConnectorOptions("mqttServer://", false, serializer, Guid.NewGuid().ToString("N"), 1883, "exceptions", "deviceStatus/device${DeviceId}", "tagRead/device${DeviceId}/tag${TagId}", "tagWrite", isPublishPartialReads));

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

        await module.StartAsync();

        try
        {
            await Task.Delay(200);

            await client.ConnectAsync(mqttClientOptions, CancellationToken.None);

            await client.SubscribeAsync(new MqttClientSubscribeOptions
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
            }, CancellationToken.None);

            await Task.Delay(200);

            lock (client)
            {
                Assert.Empty(otherMessages);
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

            await client.PublishAsync(new MqttApplicationMessageBuilder()
                .WithTopic("tagWrite")
                .WithPayload(serializer.SerializeMessage(new TagWriteRequestCommand("1", "DB22", 20, new byte[] { 1, 2, 3, 4, 5 })))
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build(), CancellationToken.None);

            Assert.True(wasWritten.WaitOne(TimeSpan.FromSeconds(2)));
        }
        finally
        {
            await module.StopAsync();

            await client.DisconnectAsync(new MqttClientDisconnectOptions()
            {
                Reason = MqttClientDisconnectOptionsReason.NormalDisconnection,
                ReasonString = string.Empty
            }, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData("json")]
    [InlineData("protobuf")]
    public async Task Test_scheduler_and_MqttServerConnector(string serializerType)
    {
        IList<TagEvent> tagEvents = new List<TagEvent>();
        IList<DeviceEvent> deviceStatusEvents = new List<DeviceEvent>();
        IList<MqttApplicationMessage> otherMessages = new List<MqttApplicationMessage>();

        var tagEvent = new ManualResetEventSlim();
        var deviceStatusEvent = new ManualResetEventSlim();
        var otherMessagesEvent = new ManualResetEventSlim();
        var connectedEvent = new ManualResetEventSlim();

        ISingleMessageSerializer serializer;
        if (serializerType == "json")
            serializer = new JsonSingleMessageSerializer();
        else if (serializerType == "protobuf")
            serializer = new ProtobufSingleMessageSerializer();
        else
            throw new InvalidOperationException("serializer not valid");

        var connector = new MqttServerConnector(new MqttServerConnectorOptions("mqttServer://", false, serializer, Guid.NewGuid().ToString("N"), 1883, "exceptions", "deviceStatus/device${DeviceId}", "tagRead/device${DeviceId}/tag${TagId}", "tagWrite", true));

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
        Assert.Empty(otherMessages);
        Assert.Empty(tagEvents);
        Assert.Empty(deviceStatusEvents);

        var connectorInterface = new Mock<ISmartIOTConnectorInterface>();

        connectorInterface.Setup(x => x.RunInitializationActionAsync(It.IsAny<Func<IList<DeviceStatusEvent>, IList<TagScheduleEvent>, Task>>()))
            .Returns(async (Func<IList<DeviceStatusEvent>, IList<TagScheduleEvent>, Task> initAction) =>
            {
                var listDeviceEvents = new List<DeviceStatusEvent>();
                var listTagEvents = new List<TagScheduleEvent>();

                if (lastDeviceStatusEvent != null)
                    listDeviceEvents.Add(lastDeviceStatusEvent);

                listTagEvents.Add(TagScheduleEvent.BuildTagData(device, tag, false));

                await initAction.Invoke(listDeviceEvents, listTagEvents);
            });

        connectorInterface.Setup(x => x.OnConnectorConnected(It.IsAny<ConnectorConnectedEventArgs>()))
            .Callback((ConnectorConnectedEventArgs e) =>
            {
                connectedEvent.Set();
            });

        var client = new MqttFactory().CreateMqttClient();

        try
        {
            await connector.StartAsync(connectorInterface.Object);

            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("localhost", 1883)
                .WithClientId("TestClient")
                .Build();

            client.ApplicationMessageReceivedAsync += eventArgs =>
            {
                if (eventArgs.ApplicationMessage.Topic.StartsWith("tagRead/"))
                {
                    tagEvents.Add(serializer.DeserializeMessage<TagEvent>(eventArgs.ApplicationMessage.PayloadSegment.Array!)!);
                    tagEvent.Set();
                }
                else if (eventArgs.ApplicationMessage.Topic.StartsWith("deviceStatus/"))
                {
                    deviceStatusEvents.Add(serializer.DeserializeMessage<DeviceEvent>(eventArgs.ApplicationMessage.PayloadSegment.Array!)!);
                    deviceStatusEvent.Set();
                }
                else
                {
                    otherMessages.Add(eventArgs.ApplicationMessage);
                    otherMessagesEvent.Set();
                }

                return Task.CompletedTask;
            };

            await client.ConnectAsync(mqttClientOptions, CancellationToken.None);

            Assert.True(connectedEvent.Wait(1000));
            Assert.Equal(WaitHandle.WaitTimeout, WaitHandle.WaitAny(new[] { tagEvent.WaitHandle, deviceStatusEvent.WaitHandle }, 1000));
            Assert.Empty(tagEvents);
            Assert.Empty(deviceStatusEvents);
            Assert.Empty(otherMessages);

            tagEvent.Reset();
            deviceStatusEvent.Reset();
            otherMessagesEvent.Reset();

            await client.SubscribeAsync(new MqttClientSubscribeOptions
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
            }, CancellationToken.None);

            Assert.True(WaitHandle.WaitAll(new[] { tagEvent.WaitHandle, deviceStatusEvent.WaitHandle }, 2000));

            Assert.Empty(otherMessages);
            Assert.Single(tagEvents); // ho ricevuto un tagEvent
            Assert.True(tagEvents.All(x => x.DeviceId == "1" && x.TagId == "DB20" && x.StartOffset == 10 && x.Data != null && x.Data.Length == 100));
            Assert.Single(deviceStatusEvents);
            Assert.True(deviceStatusEvents.All(x => x.DeviceId == "1" && x.DeviceStatus == DeviceStatus.OK && x.ErrorNumber == 0 && string.IsNullOrEmpty(x.Description)));

            engine.ScheduleNextTag(false);

            tagEvent.Reset();

            Assert.True(tagEvent.Wait(1000));

            Assert.Empty(otherMessages);
            Assert.Equal(2, tagEvents.Count); // ho ricevuto un altro tagEvent
            Assert.True(tagEvents.Take(1).All(x => x.DeviceId == "1" && x.TagId == "DB20" && x.StartOffset == 10 && x.Data != null && x.Data.Length == 100));
            Assert.True(tagEvents.Skip(1).All(x => x.DeviceId == "1" && x.TagId == "DB20" && x.StartOffset == 15 && x.Data != null && x.Data.Length == 10));
            Assert.Single(deviceStatusEvents);
            Assert.True(deviceStatusEvents.All(x => x.DeviceId == "1" && x.DeviceStatus == DeviceStatus.OK && x.ErrorNumber == 0 && string.IsNullOrEmpty(x.Description)));
        }
        finally
        {
            await connector.StopAsync();

            await client.DisconnectAsync(new MqttClientDisconnectOptions()
            {
                Reason = MqttClientDisconnectOptionsReason.NormalDisconnection,
                ReasonString = string.Empty
            }, CancellationToken.None);
        }
    }

    [Theory]
    [InlineData("json")]
    [InlineData("protobuf")]
    public async Task Test_MqttClientConnector(string serializerType)
    {
        IList<TagEvent> tagEvents = new List<TagEvent>();
        IList<DeviceEvent> deviceStatusEvents = new List<DeviceEvent>();
        IList<MqttApplicationMessage> otherMessages = new List<MqttApplicationMessage>();

        var tagEvent = new ManualResetEventSlim();
        var deviceStatusEvent = new ManualResetEventSlim();

        var mqttServerOptions = new MqttServerOptionsBuilder()
            .WithDefaultEndpoint()
            .WithDefaultEndpointPort(1883)
            .Build();

        var server = new MqttFactory().CreateMqttServer(mqttServerOptions);

        ISingleMessageSerializer serializer;
        if (serializerType == "json")
            serializer = new JsonSingleMessageSerializer();
        else if (serializerType == "protobuf")
            serializer = new ProtobufSingleMessageSerializer();
        else
            throw new InvalidOperationException("serializer not valid");

        server.InterceptingPublishAsync += eventArgs =>
        {
            lock (server) // lock to do asserts safely
            {
                if (eventArgs.ApplicationMessage.Topic.StartsWith("tagRead/"))
                {
                    tagEvents.Add(serializer.DeserializeMessage<TagEvent>(eventArgs.ApplicationMessage.PayloadSegment.Array!)!);
                    tagEvent.Set();
                }
                else if (eventArgs.ApplicationMessage.Topic.StartsWith("deviceStatus/"))
                {
                    deviceStatusEvents.Add(serializer.DeserializeMessage<DeviceEvent>(eventArgs.ApplicationMessage.PayloadSegment.Array!)!);
                    deviceStatusEvent.Set();
                }
                else
                    otherMessages.Add(eventArgs.ApplicationMessage);
            }

            return Task.CompletedTask;
        };

        var connector = new MqttClientConnector(new MqttClientConnectorOptions("mqttClient://", false, serializer, Guid.NewGuid().ToString("N"), "localhost", 1883, "exceptions", "deviceStatus/device${DeviceId}", "tagRead/device${DeviceId}/tag${TagId}", "tagWrite", TimeSpan.FromSeconds(5), "", ""));

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
            await module.StartAsync();
            await Task.Delay(1000);

            // il server parte dopo aver fatto partire il client: in questo modo sto testando anche il fatto che il client si riconnetta in automatico
            await server.StartAsync();

            if (!tagEvent.Wait(2000))
                throw new Exception("No tagReadEvent has been received");
            else if (!deviceStatusEvent.Wait(2000))
                throw new Exception("No deviceStatusEvent has been received");
            else
                await Task.Delay(2000); // wait 2 sec for otherMessages

            lock (server)
            {
                Assert.Empty(otherMessages);
                Assert.True(tagEvents.Count > 0);
                Assert.All(tagEvents, x => Assert.True(x.DeviceId == "1" && (x.TagId == "DB20" || x.TagId == "DB22") && x.StartOffset == 10 && x.Data != null && x.Data.Length == 100)); // N complete read events (MqttClientConnector always published a complete tag event)
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

            await server.InjectApplicationMessage(new InjectedMqttApplicationMessage(
                new MqttApplicationMessageBuilder()
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .WithTopic("tagWrite")
                    .WithPayload(serializer.SerializeMessage(new TagWriteRequestCommand("1", "DB22", 20, new byte[] { 1, 2, 3, 4, 5 })))
                    .Build())
            );

            Assert.True(wasWritten.WaitOne(TimeSpan.FromSeconds(2)));
        }
        finally
        {
            await module.StopAsync();
            await server.StopAsync();
        }
    }
}