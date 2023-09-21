using Moq;
using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Model;
using SmartIOT.Connector.Core.Scheduler;
using SmartIOT.Connector.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace SmartIOT.Connector.Core.Tests
{
    public class TagSchedulerEngineTests
    {
        private static (TagSchedulerEngine engine, Model.Device device, Tag tag20, Tag? tag21, Tag tag22) SetupSystem(Func<Model.Device, IDeviceDriver> driverFunc, ITimeService timeService, SchedulerConfiguration schedulerConfiguration, bool addTag21ForRead, bool pduWriteOptimizationEnabled, int singlePduReadBytes)
        {
            TagConfiguration db20 = new TagConfiguration("DB20", TagType.READ, 5, 100, 1);
            TagConfiguration db21 = new TagConfiguration("DB21", TagType.READ, 5, 100, 1);
            TagConfiguration db22 = new TagConfiguration("DB22", TagType.WRITE, 5, 100, 1);

            DeviceConfiguration deviceConfiguration = new DeviceConfiguration("", "1", true, "Device1", new List<TagConfiguration>()
            {
                db20,
                db22,
            })
            {
                IsPartialReadsEnabled = singlePduReadBytes > 0,
                IsWriteOptimizationEnabled = pduWriteOptimizationEnabled,
            };
            if (addTag21ForRead)
                deviceConfiguration.Tags.Add(db21);

            var device = new Model.Device(deviceConfiguration);

            var engine = new TagSchedulerEngine(driverFunc(device), timeService, schedulerConfiguration);

            return (engine, device, device.Tags.First(x => x.TagId == "DB20")!, device.Tags.FirstOrDefault(x => x.TagId == "DB21"), device.Tags.First(x => x.TagId == "DB22")!);
        }

        [Fact]
        public void Test_read_two_tags_cycle()
        {
            var timeService = new FakeTimeService
            {
                Now = DateTime.Now
            };

            SchedulerConfiguration schedulerConfiguration = new SchedulerConfiguration();

            (TagSchedulerEngine engine, Model.Device device, Tag tag20, Tag? tag21, Tag tag22) = SetupSystem(device => new MockDeviceDriver(device), timeService, schedulerConfiguration, true, false, 0);
            MockDeviceDriver driver = (MockDeviceDriver)engine.DeviceDriver;
            var eventListener = new FakeConnector();

            engine.TagReadEvent += eventListener.OnTagReadEvent;
            engine.TagWriteEvent += eventListener.OnTagWriteEvent;
            engine.DeviceStatusEvent += eventListener.OnDeviceStatusEvent;
            engine.ExceptionHandler += eventListener.OnException;

            // verifica stato iniziale
            Assert.Equal(DeviceStatus.UNINITIALIZED, device.DeviceStatus);
            Assert.False(tag20.IsInitialized);
            Assert.Equal(0, tag20.ErrorCode);
            Assert.False(tag21!.IsInitialized);
            Assert.Equal(0, tag21.ErrorCode);
            Assert.False(tag22.IsInitialized);
            Assert.Equal(0, tag22.ErrorCode);

            // restart driver
            Assert.True(engine.IsRestartNeeded());
            engine.RestartDriver();

            // verifica inizializzazione avvenuta
            Assert.Equal(DeviceStatus.OK, device.DeviceStatus);
            Assert.True(tag20.IsInitialized);
            Assert.Equal(0, tag20.ErrorCode);
            Assert.True(tag21!.IsInitialized);
            Assert.Equal(0, tag21.ErrorCode);
            Assert.True(tag22.IsInitialized);
            Assert.Equal(0, tag22.ErrorCode);

            // verifica eventi di inizializzazione inviati
            Assert.Equal(3, eventListener.TagReadEvents.Count);
            Assert.Single(eventListener.TagReadEvents.Where(x => x.Tag.TagId == tag20.TagId));
            Assert.Single(eventListener.TagReadEvents.Where(x => x.Tag.TagId == tag21.TagId));
            Assert.Single(eventListener.TagReadEvents.Where(x => x.Tag.TagId == tag22.TagId));
            Assert.Empty(eventListener.TagWriteEvents);
            Assert.Single(eventListener.DeviceStatusEvents.Where(x => x.DeviceStatus == DeviceStatus.OK));
            Assert.Empty(eventListener.ExceptionEvents);
            eventListener.ClearEvents();

            // verifica schedulazione regolare tag20
            var tag1 = engine.GetNextTagSchedule();
            Assert.Equal(tag20.TagId, tag1!.Tag.TagId);
            engine.ScheduleTag(tag1!);

            Assert.Single(eventListener.TagReadEvents);
            Assert.Single(eventListener.TagReadEvents.Where(x => x.Tag.TagId == tag20.TagId));
            Assert.Empty(eventListener.TagReadEvents.Where(x => x.Tag.TagId == tag21.TagId));
            Assert.Empty(eventListener.TagReadEvents.Where(x => x.Tag.TagId == tag22.TagId));
            Assert.Empty(eventListener.TagWriteEvents);
            Assert.Empty(eventListener.DeviceStatusEvents);
            Assert.Empty(eventListener.ExceptionEvents);
            eventListener.ClearEvents();

            driver.Verify(x => x.ReadTag(device, tag20, It.IsAny<byte[]>(), tag20.ByteOffset, tag20.Size), Times.Exactly(2)); // read invocata 2 volte perché la DB viene anche inizializzata

            // verifica schedulazione regolare tag21
            var tag2 = engine.GetNextTagSchedule();
            Assert.Equal(tag21.TagId, tag2!.Tag.TagId);
            engine.ScheduleTag(tag2!);

            Assert.Single(eventListener.TagReadEvents);
            Assert.Empty(eventListener.TagReadEvents.Where(x => x.Tag.TagId == tag20.TagId));
            Assert.Single(eventListener.TagReadEvents.Where(x => x.Tag.TagId == tag21.TagId));
            Assert.Empty(eventListener.TagReadEvents.Where(x => x.Tag.TagId == tag22.TagId));
            Assert.Empty(eventListener.TagWriteEvents);
            Assert.Empty(eventListener.DeviceStatusEvents);
            Assert.Empty(eventListener.ExceptionEvents);
            eventListener.ClearEvents();

            driver.Verify(x => x.ReadTag(device, tag21, It.IsAny<byte[]>(), tag21.ByteOffset, tag21.Size), Times.Exactly(2)); // read invocata 2 volte perché la DB viene anche inizializzata

            // verifica scrittura di tutto il tag22
            tag22.IsWriteSynchronizationRequested = true;

            var tag3 = engine.GetNextTagSchedule();
            Assert.Equal(tag22.TagId, tag3!.Tag.TagId);
            engine.ScheduleTag(tag3!);

            Assert.Empty(eventListener.TagReadEvents);
            Assert.Single(eventListener.TagWriteEvents);
            Assert.Single(eventListener.TagWriteEvents.Where(x => x.Tag.TagId == tag22.TagId));
            Assert.Empty(eventListener.DeviceStatusEvents);
            Assert.Empty(eventListener.ExceptionEvents);
            eventListener.ClearEvents();

            driver.Verify(x => x.WriteTag(device, tag22, It.IsAny<byte[]>(), 5, 100), Times.Once);
            Assert.False(tag22.IsWriteSynchronizationRequested);

            // verifica schedulazione regolare tag20
            var tag4 = engine.GetNextTagSchedule();
            Assert.Equal(tag20.TagId, tag4!.Tag.TagId);
            engine.ScheduleTag(tag4!);

            Assert.Single(eventListener.TagReadEvents);
            Assert.Single(eventListener.TagReadEvents.Where(x => x.Tag.TagId == tag20.TagId));
            Assert.Empty(eventListener.TagReadEvents.Where(x => x.Tag.TagId == tag21.TagId));
            Assert.Empty(eventListener.TagReadEvents.Where(x => x.Tag.TagId == tag22.TagId));
            Assert.Empty(eventListener.TagWriteEvents);
            Assert.Empty(eventListener.DeviceStatusEvents);
            Assert.Empty(eventListener.ExceptionEvents);
            eventListener.ClearEvents();

            driver.Verify(x => x.ReadTag(device, tag20, It.IsAny<byte[]>(), tag20.ByteOffset, tag20.Size), Times.Exactly(3));
        }

        [Fact]
        public void Test_partial_reads()
        {
            var timeService = new FakeTimeService
            {
                Now = DateTime.Now
            };

            SchedulerConfiguration schedulerConfiguration = new SchedulerConfiguration();

            (TagSchedulerEngine engine, Model.Device device, Tag tag20, Tag? _, Tag tag22) = SetupSystem(device => new MockDeviceDriver(device), timeService, schedulerConfiguration, false, false, 25);
            MockDeviceDriver driver = (MockDeviceDriver)engine.DeviceDriver;
            device.SinglePDUReadBytes = 25;

            var eventListener = new FakeConnector();

            engine.TagReadEvent += eventListener.OnTagReadEvent;
            engine.TagWriteEvent += eventListener.OnTagWriteEvent;
            engine.DeviceStatusEvent += eventListener.OnDeviceStatusEvent;
            engine.ExceptionHandler += eventListener.OnException;

            // verifica stato iniziale
            Assert.Equal(DeviceStatus.UNINITIALIZED, device.DeviceStatus);
            Assert.False(tag20.IsInitialized);
            Assert.Equal(0, tag20.ErrorCode);
            Assert.False(tag22.IsInitialized);
            Assert.Equal(0, tag22.ErrorCode);

            // restart driver
            Assert.True(engine.IsRestartNeeded());
            engine.RestartDriver();

            // verifica inizializzazione avvenuta
            Assert.Equal(DeviceStatus.OK, device.DeviceStatus);
            Assert.True(tag20.IsInitialized);
            Assert.Equal(0, tag20.ErrorCode);
            Assert.True(tag22.IsInitialized);
            Assert.Equal(0, tag22.ErrorCode);

            // verifica eventi di inizializzazione inviati
            Assert.Equal(2, eventListener.TagReadEvents.Count);
            Assert.Single(eventListener.TagReadEvents.Where(x => x.Tag.TagId == tag20.TagId));
            Assert.Single(eventListener.TagReadEvents.Where(x => x.Tag.TagId == tag22.TagId));
            Assert.Empty(eventListener.TagWriteEvents);
            Assert.Single(eventListener.DeviceStatusEvents.Where(x => x.DeviceStatus == DeviceStatus.OK));
            Assert.Empty(eventListener.ExceptionEvents);
            eventListener.ClearEvents();
            driver.ResetInvocations();

            // verifica schedulazione regolare tag20 inviando più eventi di lettura parziale
            var t20 = engine.GetNextTagSchedule();
            Assert.Equal(tag20.TagId, t20!.Tag.TagId);
            engine.ScheduleTag(t20!);

            Assert.Single(eventListener.TagReadEvents);
            Assert.Single(eventListener.TagReadEvents.Where(x => x.Tag.TagId == tag20.TagId && x.ErrorNumber == 0 && x.Data!.Length == 0));
            Assert.Empty(eventListener.TagWriteEvents);
            Assert.Empty(eventListener.DeviceStatusEvents);
            Assert.Empty(eventListener.ExceptionEvents);
            driver.Verify(x => x.ReadTag(device, tag20, It.IsAny<byte[]>(), tag20.ByteOffset + 0 * device.SinglePDUReadBytes, device.SinglePDUReadBytes), Times.Exactly(1));
            driver.Verify(x => x.ReadTag(device, tag20, It.IsAny<byte[]>(), tag20.ByteOffset + 1 * device.SinglePDUReadBytes, device.SinglePDUReadBytes), Times.Exactly(1));
            driver.Verify(x => x.ReadTag(device, tag20, It.IsAny<byte[]>(), tag20.ByteOffset + 2 * device.SinglePDUReadBytes, device.SinglePDUReadBytes), Times.Exactly(1));
            driver.Verify(x => x.ReadTag(device, tag20, It.IsAny<byte[]>(), tag20.ByteOffset + 3 * device.SinglePDUReadBytes, device.SinglePDUReadBytes), Times.Exactly(1));

            eventListener.ClearEvents();
            driver.ResetInvocations();

            // verifica di lettura parziale intervallata da scrittura
            // quando viene letto il tag20, simulo la richiesta di scrittura
            driver.ReadTagCallback = (data, startOffset, length) =>
            {
                tag22.RequestTagWrite(new byte[] { 100, 101 }, 10);
                driver.ReadTagCallback = null; // autoreset alla prima invocazione
            };

            t20 = engine.GetNextTagSchedule();
            Assert.Equal(tag20.TagId, t20!.Tag.TagId);
            engine.ScheduleTag(t20!);

            Assert.Empty(eventListener.TagReadEvents);
            Assert.Empty(eventListener.TagWriteEvents);
            Assert.Empty(eventListener.DeviceStatusEvents);
            Assert.Empty(eventListener.ExceptionEvents);
            driver.Verify(x => x.ReadTag(device, tag20, It.IsAny<byte[]>(), tag20.ByteOffset + 0 * device.SinglePDUReadBytes, device.SinglePDUReadBytes), Times.Exactly(1));

            eventListener.ClearEvents();
            driver.ResetInvocations();

            // a questo punto l'engine deve schedulare una scrittura perché è intervenuta nel mentre
            var t22 = engine.GetNextTagSchedule();
            Assert.Equal(tag22.TagId, t22!.Tag.TagId);
            engine.ScheduleTag(t22!);

            Assert.Empty(eventListener.TagReadEvents);
            Assert.Single(eventListener.TagWriteEvents);
            Assert.Single(eventListener.TagWriteEvents.Where(x => x.Tag.TagId == tag22.TagId));
            Assert.Empty(eventListener.DeviceStatusEvents);
            Assert.Empty(eventListener.ExceptionEvents);
            driver.Verify(x => x.WriteTag(device, tag22, It.IsAny<byte[]>(), 10, 2), Times.Once);
            Assert.False(tag22.IsWriteSynchronizationRequested);

            eventListener.ClearEvents();
            driver.ResetInvocations();

            // ripresa della schedulazione regolare tag20 da dove si era interrotta prima
            t20 = engine.GetNextTagSchedule();
            Assert.Equal(tag20.TagId, t20!.Tag.TagId);
            engine.ScheduleTag(t20!);

            Assert.Single(eventListener.TagReadEvents);
            Assert.Single(eventListener.TagReadEvents.Where(x => x.Tag.TagId == tag20.TagId));
            Assert.Empty(eventListener.TagWriteEvents);
            Assert.Empty(eventListener.DeviceStatusEvents);
            Assert.Empty(eventListener.ExceptionEvents);
            driver.Verify(x => x.ReadTag(device, tag20, It.IsAny<byte[]>(), tag20.ByteOffset + 1 * device.SinglePDUReadBytes, device.SinglePDUReadBytes), Times.Exactly(1));
            driver.Verify(x => x.ReadTag(device, tag20, It.IsAny<byte[]>(), tag20.ByteOffset + 2 * device.SinglePDUReadBytes, device.SinglePDUReadBytes), Times.Exactly(1));
            driver.Verify(x => x.ReadTag(device, tag20, It.IsAny<byte[]>(), tag20.ByteOffset + 3 * device.SinglePDUReadBytes, device.SinglePDUReadBytes), Times.Exactly(1));

            eventListener.ClearEvents();
            driver.ResetInvocations();
        }

        [Theory]
        [InlineData(false, 0)]
        [InlineData(true, 5)]
        public void Test_read_write_cycle(bool pduWriteOptimizationEnabled, int singlePduWriteBytes)
        {
            var timeService = new FakeTimeService
            {
                Now = DateTime.Now
            };

            SchedulerConfiguration schedulerConfiguration = new SchedulerConfiguration()
            {
                WaitTimeBetweenEveryScheduleMillis = 100,
                WaitTimeBetweenReadSchedulesMillis = 200,
            };

            (TagSchedulerEngine engine, Model.Device device, Tag tag20, Tag? _, Tag tag22) = SetupSystem(device => new MockDeviceDriver(device), timeService, schedulerConfiguration, false, pduWriteOptimizationEnabled, 0);
            MockDeviceDriver driver = (MockDeviceDriver)engine.DeviceDriver;
            device.SinglePDUWriteBytes = singlePduWriteBytes;

            var eventListener = new FakeConnector();

            engine.TagReadEvent += eventListener.OnTagReadEvent;
            engine.TagWriteEvent += eventListener.OnTagWriteEvent;
            engine.DeviceStatusEvent += eventListener.OnDeviceStatusEvent;
            engine.ExceptionHandler += eventListener.OnException;

            // verifica stato iniziale
            Assert.Equal(DeviceStatus.UNINITIALIZED, device.DeviceStatus);
            Assert.False(tag20.IsInitialized);
            Assert.Equal(0, tag20.ErrorCode);
            Assert.False(tag22.IsInitialized);
            Assert.Equal(0, tag22.ErrorCode);

            // verifica di errore durante prima inizializzazione
            driver.StartInterfaceReturns = 1;

            Assert.True(engine.IsRestartNeeded());
            engine.RestartDriver();

            Assert.Equal(DeviceStatus.ERROR, device.DeviceStatus);
            Assert.Equal(1, device.ErrorCode);
            Assert.False(tag20.IsInitialized);
            Assert.Equal(0, tag20.ErrorCode);
            Assert.False(tag22.IsInitialized);
            Assert.Equal(0, tag22.ErrorCode);

            Assert.Empty(eventListener.TagReadEvents);
            Assert.Empty(eventListener.TagWriteEvents);
            Assert.Single(eventListener.DeviceStatusEvents);
            Assert.Single(eventListener.DeviceStatusEvents.Where(x => x.DeviceStatus == DeviceStatus.ERROR && x.ErrorCode == 1));
            Assert.Empty(eventListener.ExceptionEvents);
            eventListener.ClearEvents();

            driver.StartInterfaceCallback = () => throw new Exception("This is a test");

            // verifica di non procedere a una nuova inizializzazione se non è trascorso abbastanza tempo
            Assert.False(engine.IsRestartNeeded());
            Assert.Throws<TagSchedulerWaitException>(() => engine.GetNextTagSchedule());

            // verifica inizializzazione ok
            timeService.Now += schedulerConfiguration.RestartDeviceInErrorTimeout;
            Assert.True(engine.IsRestartNeeded());
            engine.RestartDriver();

            Assert.Equal(DeviceStatus.ERROR, device.DeviceStatus);
            Assert.Equal(-100, device.ErrorCode);
            Assert.False(tag20.IsInitialized);
            Assert.Equal(0, tag20.ErrorCode);
            Assert.False(tag22.IsInitialized);
            Assert.Equal(0, tag22.ErrorCode);

            Assert.Empty(eventListener.TagReadEvents);
            Assert.Empty(eventListener.TagWriteEvents);
            Assert.Single(eventListener.DeviceStatusEvents);
            Assert.Single(eventListener.DeviceStatusEvents.Where(x => x.DeviceStatus == DeviceStatus.ERROR && x.ErrorCode == -100 && x.Description == "This is a test"));
            Assert.Empty(eventListener.ExceptionEvents);
            eventListener.ClearEvents();

            // verifica errore su tag durante inizializzazione
            driver.StartInterfaceCallback = null;
            driver.StartInterfaceReturns = 0;
            driver.ReadTagReturns = 1;

            timeService.Now += schedulerConfiguration.RestartDeviceInErrorTimeout;
            Assert.True(engine.IsRestartNeeded());
            engine.RestartDriver();

            Assert.Throws<TagSchedulerWaitException>(() => engine.GetNextTagSchedule()); // nessun tag schedulabile

            Assert.Equal(DeviceStatus.OK, device.DeviceStatus);
            Assert.False(tag20.IsInitialized);
            Assert.Equal(1, tag20.ErrorCode);
            Assert.False(tag22.IsInitialized);
            Assert.Equal(1, tag22.ErrorCode);

            Assert.Equal(2, eventListener.TagReadEvents.Count);
            Assert.Single(eventListener.TagReadEvents.Where(x => x.Tag.TagId == tag20.TagId && x.ErrorNumber == 1));
            Assert.Single(eventListener.TagReadEvents.Where(x => x.Tag.TagId == tag22.TagId && x.ErrorNumber == 1));
            Assert.Empty(eventListener.TagWriteEvents);
            Assert.Single(eventListener.DeviceStatusEvents);
            Assert.Single(eventListener.DeviceStatusEvents.Where(x => x.Device.DeviceId == device.DeviceId && x.ErrorCode == 0));
            Assert.Empty(eventListener.ExceptionEvents);
            eventListener.ClearEvents();

            // verifica inizializzazione ok
            driver.ReadTagReturns = 0;
            timeService.Now += schedulerConfiguration.RestartDeviceInErrorTimeout;
            Assert.True(engine.IsRestartNeeded());

            engine.RestartDriver();

            Assert.Equal(DeviceStatus.OK, device.DeviceStatus);
            Assert.True(tag20.IsInitialized);
            Assert.Equal(0, tag20.ErrorCode);
            Assert.True(tag22.IsInitialized);
            Assert.Equal(0, tag22.ErrorCode);

            Assert.Equal(2, eventListener.TagReadEvents.Count);
            Assert.Single(eventListener.TagReadEvents.Where(x => x.Tag.TagId == tag20.TagId && x.ErrorNumber == 0));
            Assert.Single(eventListener.TagReadEvents.Where(x => x.Tag.TagId == tag22.TagId && x.ErrorNumber == 0));
            Assert.Empty(eventListener.TagWriteEvents);
            Assert.Single(eventListener.DeviceStatusEvents);
            Assert.Single(eventListener.DeviceStatusEvents.Where(x => x.Device.DeviceId == device.DeviceId && x.ErrorCode == 0));
            Assert.Empty(eventListener.ExceptionEvents);
            eventListener.ClearEvents();

            // verifica di non schedulare nulla se non passa abbastanza tempo
            Assert.Throws<TagSchedulerWaitException>(() => engine.GetNextTagSchedule());
            timeService.Now += schedulerConfiguration.WaitTimeBetweenEverySchedule;
            Assert.Throws<TagSchedulerWaitException>(() => engine.GetNextTagSchedule());
            timeService.Now += schedulerConfiguration.WaitTimeBetweenReadSchedules;

            // una volta che i tag sono inizializzati, verifica di non inviare nessun evento se non cambia nessun bit
            for (int i = 0; i < 10; i++)
            {
                timeService.Now += schedulerConfiguration.WaitTimeBetweenReadSchedules;

                var t = engine.GetNextTagSchedule();
                Assert.Equal(tag20, t.Tag);

                engine.ScheduleTag(t);

                Assert.Single(eventListener.TagReadEvents);
                Assert.Single(eventListener.TagReadEvents.Where(x => x.Tag.TagId == tag20.TagId && x.ErrorNumber == 0 && x.Data!.Length == 0));
                Assert.Empty(eventListener.TagWriteEvents);
                Assert.Empty(eventListener.DeviceStatusEvents);
                Assert.Empty(eventListener.ExceptionEvents);
                eventListener.ClearEvents();
            }

            // verifico di inviare un evento di lettura se cambia un bit
            driver.ReadTagCallback = (data, startOffset, length) =>
            {
                data[10] = 99;
            };

            timeService.Now += schedulerConfiguration.WaitTimeBetweenReadSchedules;
            var t20 = engine.GetNextTagSchedule();
            Assert.Equal(tag20, t20.Tag);

            engine.ScheduleTag(t20);

            Assert.Single(eventListener.TagReadEvents);
            Assert.Single(eventListener.TagReadEvents.Where(x => x.Tag.TagId == tag20.TagId && x.ErrorNumber == 0 && x.Data!.Length == 1 && x.Data[0] == 99 && x.StartOffset == 15));
            Assert.Empty(eventListener.TagWriteEvents);
            Assert.Empty(eventListener.DeviceStatusEvents);
            Assert.Empty(eventListener.ExceptionEvents);
            eventListener.ClearEvents();

            driver.ReadTagCallback = null;

            // verifica che le letture successive non inviano eventi
            for (int i = 0; i < 10; i++)
            {
                timeService.Now += schedulerConfiguration.WaitTimeBetweenReadSchedules;
                var t = engine.GetNextTagSchedule();
                Assert.Equal(tag20, t.Tag);

                engine.ScheduleTag(t);

                Assert.Single(eventListener.TagReadEvents);
                Assert.Single(eventListener.TagReadEvents.Where(x => x.Tag.TagId == tag20.TagId && x.ErrorNumber == 0 && x.Data!.Length == 0));
                Assert.Empty(eventListener.TagWriteEvents);
                Assert.Empty(eventListener.DeviceStatusEvents);
                Assert.Empty(eventListener.ExceptionEvents);
                eventListener.ClearEvents();
            }

            // verifica di scrittura in errore
            tag22.RequestTagWrite(new byte[] { 33 }, 10); // richiesta di modifica dati

            driver.WriteReturns = 1;

            for (int i = 0; i < 10; i++)
            {
                var t = engine.GetNextTagSchedule();
                Assert.Equal(tag22, t.Tag);

                engine.ScheduleTag(t);

                Assert.Empty(eventListener.TagReadEvents);
                Assert.Single(eventListener.TagWriteEvents);
                Assert.Single(eventListener.TagWriteEvents.Where(x => x.Tag.TagId == tag22.TagId && x.ErrorNumber == 1));
                Assert.Empty(eventListener.DeviceStatusEvents);
                Assert.Empty(eventListener.ExceptionEvents);
                driver.Verify(x => x.WriteTag(device, tag22, It.IsAny<byte[]>(), 10, 1));
                Assert.True(tag22.IsWriteSynchronizationRequested);

                eventListener.ClearEvents();
                driver.ResetInvocations();

                timeService.Now += schedulerConfiguration.WaitTimeAfterError;
            }

            // verifica di scrittura tag22 con i bounds attesi
            driver.WriteReturns = 0;

            var t22 = engine.GetNextTagSchedule();
            Assert.Equal(tag22, t22.Tag);

            engine.ScheduleTag(t22);

            Assert.Empty(eventListener.TagReadEvents);
            Assert.Single(eventListener.TagWriteEvents);
            Assert.Single(eventListener.TagWriteEvents.Where(x => x.Tag.TagId == tag22.TagId && x.StartOffset == 10 && x.Data!.Length == 1 && x.Data[0] == 33));
            Assert.Empty(eventListener.DeviceStatusEvents);
            Assert.Empty(eventListener.ExceptionEvents);
            driver.Verify(x => x.WriteTag(device, tag22, It.IsAny<byte[]>(), 10, 1));

            eventListener.ClearEvents();
            driver.ResetInvocations();

            // verifica che le letture successive non inviano eventi
            for (int i = 0; i < 10; i++)
            {
                timeService.Now += schedulerConfiguration.WaitTimeBetweenReadSchedules;
                var t = engine.GetNextTagSchedule();
                Assert.Equal(tag20, t.Tag);

                engine.ScheduleTag(t);

                Assert.Single(eventListener.TagReadEvents);
                Assert.Single(eventListener.TagReadEvents.Where(x => x.Tag.TagId == tag20.TagId && x.ErrorNumber == 0 && x.Data!.Length == 0));
                Assert.Empty(eventListener.TagWriteEvents);
                Assert.Empty(eventListener.DeviceStatusEvents);
                Assert.Empty(eventListener.ExceptionEvents);
                eventListener.ClearEvents();
            }

            // verifica di scrittura ottimizzata a seconda della dimensione della PDU
            driver.ResetInvocations();

            tag22.RequestTagWrite(new byte[] { 100, 101, 102 }, 10);
            tag22.RequestTagWrite(new byte[] { 200, 201 }, 20);

            t22 = engine.GetNextTagSchedule();
            Assert.Equal(tag22, t22.Tag);

            engine.ScheduleTag(t22);

            Assert.Empty(eventListener.TagReadEvents);
            Assert.Single(eventListener.TagWriteEvents);
            Assert.Single(eventListener.TagWriteEvents.Where(x => x.Tag.TagId == tag22.TagId && x.StartOffset == 10 && x.Data!.Length == 12 && x.Data[0] == 100 && x.Data[1] == 101 && x.Data[2] == 102 && x.Data[10] == 200 && x.Data[11] == 201));
            Assert.Empty(eventListener.DeviceStatusEvents);
            Assert.Empty(eventListener.ExceptionEvents);

            if (pduWriteOptimizationEnabled)
            {
                driver.Verify(x => x.WriteTag(device, tag22, It.IsAny<byte[]>(), 10, 3));
                driver.Verify(x => x.WriteTag(device, tag22, It.IsAny<byte[]>(), 20, 2));
            }
            else
            {
                driver.Verify(x => x.WriteTag(device, tag22, It.IsAny<byte[]>(), 10, 12));
            }

            driver.VerifyNoOtherCalls();

            eventListener.ClearEvents();
            driver.ResetInvocations();
        }

        [Fact]
        public void Test_reconnect_on_error()
        {
            var timeService = new FakeTimeService
            {
                Now = DateTime.Now
            };

            SchedulerConfiguration configuration = new SchedulerConfiguration();

            (TagSchedulerEngine engine, Model.Device device, Tag tag20, _, Tag tag22) = SetupSystem(device => new MockDeviceDriver(device), timeService, configuration, false, false, 0);
            MockDeviceDriver driver = (MockDeviceDriver)engine.DeviceDriver;

            Assert.True(engine.IsRestartNeeded());
            engine.RestartDriver();

            Assert.Equal(DeviceStatus.OK, device.DeviceStatus);
            Assert.True(tag20.IsInitialized);
            Assert.Equal(0, tag20.ErrorCode);
            Assert.True(tag22.IsInitialized);
            Assert.Equal(0, tag22.ErrorCode);

            timeService.Now += configuration.RestartDeviceInErrorTimeout;
            Assert.False(engine.IsRestartNeeded()); // non devo far ripartire nulla perché non ci sono errori

            driver.ReadTagReturns = 1;

            for (int i = 0; i < configuration.MaxErrorsBeforeReconnection; i++)
            {
                var tag = engine.GetNextTagSchedule();
                Assert.NotNull(tag);

                engine.ScheduleTag(tag!);
                Assert.NotEqual(0, tag!.Tag.ErrorCode);
                Assert.Equal(i + 1, tag.Tag.ErrorCount);

                Assert.Throws<TagSchedulerWaitException>(() => engine.GetNextTagSchedule());
                timeService.Now += configuration.WaitTimeAfterError;
            }

            Assert.NotEqual(0, tag20.ErrorCode);
            Assert.Equal(configuration.MaxErrorsBeforeReconnection, tag20.ErrorCount);

            driver.ReadTagReturns = 0;

            Assert.True(engine.IsRestartNeeded()); // eseguiti N tentativi in errore
            engine.RestartDriver();

            Assert.Equal(DeviceStatus.OK, device.DeviceStatus);
            Assert.True(tag20.IsInitialized);
            Assert.Equal(0, tag20.ErrorCode);
            Assert.True(tag22.IsInitialized);
            Assert.Equal(0, tag22.ErrorCode);
        }
    }
}