using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Model;
using SmartIOT.Connector.Core.Scheduler;
using SmartIOT.Connector.Mocks;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

namespace SmartIOT.Connector.Core.Tests
{
	public class TagSchedulerTests
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
		public void Test_scheduler()
		{
			var timeService = new TimeService();

			SchedulerConfiguration schedulerConfiguration = new SchedulerConfiguration();

			(TagSchedulerEngine engine, Model.Device device, Tag tag20, Tag? tag21, Tag tag22) = SetupSystem(device => new MockDeviceDriver(device), timeService, schedulerConfiguration, true, false, 0);

			var schedulerEventListener = new FakeConnector();
			var engineEventListener = new FakeConnector();

			var scheduler = new TagScheduler("TestScheduler", engine, timeService);

			scheduler.TagReadEvent += schedulerEventListener.OnTagReadEvent;
			scheduler.TagWriteEvent += schedulerEventListener.OnTagWriteEvent;
			scheduler.DeviceStatusEvent += schedulerEventListener.OnDeviceStatusEvent;
			scheduler.ExceptionHandler += schedulerEventListener.OnException;

			engine.TagReadEvent += engineEventListener.OnTagReadEvent;
			engine.TagWriteEvent += engineEventListener.OnTagWriteEvent;
			engine.DeviceStatusEvent += engineEventListener.OnDeviceStatusEvent;
			engine.ExceptionHandler += engineEventListener.OnException;

			scheduler.Start();
			Thread.Sleep(1000);
			scheduler.Stop();

			Assert.True(schedulerEventListener.TagReadEvents.Count > 0);
			Assert.Empty(schedulerEventListener.TagWriteEvents);
			Assert.True(schedulerEventListener.DeviceStatusEvents.Count > 0);
			Assert.Empty(schedulerEventListener.ExceptionEvents);

			var e = schedulerEventListener.ExceptionEvents.FirstOrDefault();
			if (e != null)
				throw new System.Exception(e.ToString());

			Assert.True(engineEventListener.TagReadEvents.Count > 0);
			Assert.Empty(engineEventListener.TagWriteEvents);
			Assert.True(engineEventListener.DeviceStatusEvents.Count > 0);
			Assert.Empty(engineEventListener.ExceptionEvents);
		}
	}
}
