using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Mocks;
using SmartIOT.Connector.Plc.S7Net;
using SmartIOT.Connector.Plc.Snap7;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SmartIOT.Connector.Core.Tests
{
    public class SmartIotConnectorTests
    {
        [Fact]
        public void Serialize_module_configuration_json()
        {
            SmartIotConnectorConfiguration d = new SmartIotConnectorConfiguration()
            {
                SchedulerConfiguration = new SchedulerConfiguration(),
                DeviceConfigurations = new List<DeviceConfiguration>()
                {
                    new DeviceConfiguration("", "1", true, "Test Device", new List<TagConfiguration>()
                    {
                        new TagConfiguration("DB20", TagType.READ, 0, 100, 1),
                        new TagConfiguration("DB22", TagType.WRITE, 0, 100, 1),
                    })
                },
                ConnectorConnectionStrings = new List<string>()
                {
                    "fake://fake"
                }
            };

            string s = JsonSerializer.Serialize(d, new JsonSerializerOptions());
            File.WriteAllText("driver_serialized.json", s);

            Assert.True(true, "this is not a real test indeed");
        }

        [Fact]
        public void Deserialize_driver_configuration()
        {
            var d = SmartIotConnectorConfiguration.FromJson(File.ReadAllText("driver.json"));

            Assert.NotNull(d);
            Assert.Single(d!.DeviceConfigurations);
            Assert.Single(d.DeviceConfigurations);

            DeviceConfiguration pc = d.DeviceConfigurations[0];
            Assert.Equal("1", pc.DeviceId);
            Assert.Equal(2, pc.Tags.Count);
            var t0 = pc.Tags[0];
            var t1 = pc.Tags[1];

            Assert.Equal("DB20", t0.TagId);
            Assert.Equal("DB22", t1.TagId);
        }

        [Fact]
        public void Build_driver_module()
        {
            var module = new SmartIotConnectorBuilder()
                .WithAutoDiscoverDeviceDriverFactories()
                .WithAutoDiscoverConnectorFactories()
                .WithConfigurationJsonFilePath("driver2.json")
                .Build();

            var drivers = module.Schedulers;
            Assert.Equal(2, drivers.Count);

            var d0 = drivers[0];
            var d1 = drivers[1];
            Assert.IsType<Snap7Driver>(d0.DeviceDriver);
            Assert.IsType<S7NetDriver>(d1.DeviceDriver);

            Assert.NotNull(d0.Device);
            var p0 = d0.Device;

            Assert.Equal(2, p0.Tags.Count);

            Assert.Single(module.Connectors);
            Assert.True(module.Connectors[0] is FakeConnector);
        }

        [Fact]
        public async Task Test_module_connector_tag_write()
        {
            var module = new SmartIotConnectorBuilder()
                .WithAutoDiscoverDeviceDriverFactories()
                .WithAutoDiscoverConnectorFactories()
                .WithConfigurationJsonFilePath("test-config.json")
                .Build();

            MockDeviceDriver driver = (MockDeviceDriver)module.Schedulers[0].DeviceDriver;
            var device = driver.Device;
            Assert.NotNull(device.Tags.Single(x => x.TagId == "DB22"));

            FakeConnector c = (FakeConnector)module.Connectors[0];

            var wasRead = new AutoResetEvent(false);
            var wasWritten = new AutoResetEvent(false);

            module.TagReadEvent += (s, e) =>
            {
                if (e.TagScheduleEvent.Tag.TagId == "DB22")
                    wasRead.Set();
            };
            module.TagWriteEvent += (s, e) =>
            {
                if (e.TagScheduleEvent.Tag.TagId == "DB22")
                    wasWritten.Set();
            };

            try
            {
                await module.StartAsync();

                // attendo l'inizializzazione
                Assert.True(wasRead.WaitOne(TimeSpan.FromSeconds(2)));

                // test di scrittura
                c.RequestTagWrite("1", "DB22", 10, new byte[] { 1, 2, 3, 4, 5 });

                Assert.True(wasWritten.WaitOne(TimeSpan.FromSeconds(2)));

                Assert.Single(c.TagWriteEvents);
                var write = c.TagWriteEvents[0];

                Assert.Equal("DB22", write.Tag.TagId);
                Assert.Equal(10, write.StartOffset);
                Assert.Equal(5, write.Data!.Length);
            }
            finally
            {
                await module.StopAsync();
            }
        }

        [Fact]
        public async Task Test_module_connector_aggregating_tag_write_events()
        {
            var module = new SmartIotConnectorBuilder()
                .WithAutoDiscoverDeviceDriverFactories()
                .WithAutoDiscoverConnectorFactories()
                .WithConfigurationJsonFilePath("test-config.json")
                .Build();

            Scheduler.ITagScheduler scheduler = module.Schedulers[0];
            MockDeviceDriver driver = (MockDeviceDriver)scheduler.DeviceDriver;
            var device = driver.Device;
            Assert.NotNull(device.Tags.Single(x => x.TagId == "DB22"));

            FakeConnector c = (FakeConnector)module.Connectors[0];

            var wasRead = new AutoResetEvent(false);
            var wasWritten = new AutoResetEvent(false);

            module.TagReadEvent += (s, e) =>
            {
                if (e.TagScheduleEvent.Tag.TagId == "DB22")
                    wasRead.Set();
            };
            module.TagWriteEvent += (s, e) =>
            {
                if (e.TagScheduleEvent.Tag.TagId == "DB22")
                    wasWritten.Set();
            };

            try
            {
                await module.StartAsync();

                // attendo l'inizializzazione
                Assert.True(wasRead.WaitOne(TimeSpan.FromSeconds(2)));

                scheduler.IsPaused = true;
                await Task.Delay(100);

                // test di scrittura multipli aggregabili
                c.RequestTagWrite("1", "DB22", 10, new byte[] { 1, 2, 3, 4, 5 });
                c.RequestTagWrite("1", "DB22", 20, new byte[] { 11, 12, 13, 14, 15 });

                scheduler.IsPaused = false;

                Assert.True(wasWritten.WaitOne(TimeSpan.FromSeconds(2)));

                Assert.Single(c.TagWriteEvents);
                var write = c.TagWriteEvents[0];

                Assert.Equal("DB22", write.Tag.TagId);
                Assert.Equal(10, write.StartOffset);
                Assert.Equal(15, write.Data!.Length);
            }
            finally
            {
                await module.StopAsync();
            }
        }
    }
}
