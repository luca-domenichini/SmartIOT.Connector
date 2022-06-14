using SmartIOT.Connector.Core.Connector;
using SmartIOT.Connector.Core.Events;
using SmartIOT.Connector.Device.Mocks;
using Xunit;

namespace SmartIOT.Connector.Core.Tests
{
	public class ConnectorEventQueueTests
	{
		[Fact]
		public void Test_aggregating_tagStatus()
		{
			Model.Device device = new Model.Device(new Conf.DeviceConfiguration());
			Model.Tag t20 = new Model.Tag(new Conf.TagConfiguration("DB20", Conf.TagType.READ, 10, 100, 1));

			var driver = new MockDeviceDriver(device);

			var queue = new AggregatingConnectorEventQueue();

			Assert.Null(queue.PopOrDefault());

			queue.Push(CompositeConnectorEvent.TagRead((null, new TagScheduleEventArgs(driver, TagScheduleEvent.BuildTagStatus(device, t20, 0, "0", false)))));
			queue.Push(CompositeConnectorEvent.TagRead((null, new TagScheduleEventArgs(driver, TagScheduleEvent.BuildTagStatus(device, t20, 1, "1", false)))));

			var e = queue.PopOrDefault();
			Assert.NotNull(e?.TagReadScheduleEvent);
			Assert.Equal(device, e!.TagReadScheduleEvent!.Value.args.TagScheduleEvent.Device);
			Assert.Equal(t20, e!.TagReadScheduleEvent!.Value.args.TagScheduleEvent.Tag);
			Assert.Equal(1, e!.TagReadScheduleEvent!.Value.args.TagScheduleEvent.ErrorNumber);
			Assert.Equal("1", e!.TagReadScheduleEvent!.Value.args.TagScheduleEvent.Description);

			Assert.Null(queue.PopOrDefault());
		}

		[Fact]
		public void Test_not_aggregating_tagStatus()
		{
			Model.Device device = new Model.Device(new Conf.DeviceConfiguration());
			Model.Tag t20 = new Model.Tag(new Conf.TagConfiguration("DB20", Conf.TagType.READ, 10, 100, 1));
			Model.Tag t22 = new Model.Tag(new Conf.TagConfiguration("DB20", Conf.TagType.READ, 10, 100, 1));

			var driver = new MockDeviceDriver(device);

			var queue = new AggregatingConnectorEventQueue();

			Assert.Null(queue.PopOrDefault());

			queue.Push(CompositeConnectorEvent.TagRead((null, new TagScheduleEventArgs(driver, TagScheduleEvent.BuildTagStatus(device, t20, 0, "0", false)))));
			queue.Push(CompositeConnectorEvent.TagRead((null, new TagScheduleEventArgs(driver, TagScheduleEvent.BuildTagStatus(device, t22, 1, "1", false)))));

			var e = queue.PopOrDefault();
			Assert.NotNull(e?.TagReadScheduleEvent);
			Assert.Equal(device, e!.TagReadScheduleEvent!.Value.args.TagScheduleEvent.Device);
			Assert.Equal(t20, e!.TagReadScheduleEvent!.Value.args.TagScheduleEvent.Tag);
			Assert.Equal(0, e!.TagReadScheduleEvent!.Value.args.TagScheduleEvent.ErrorNumber);
			Assert.Equal("0", e!.TagReadScheduleEvent!.Value.args.TagScheduleEvent.Description);

			e = queue.PopOrDefault();
			Assert.NotNull(e?.TagReadScheduleEvent);
			Assert.Equal(device, e!.TagReadScheduleEvent!.Value.args.TagScheduleEvent.Device);
			Assert.Equal(t22, e!.TagReadScheduleEvent!.Value.args.TagScheduleEvent.Tag);
			Assert.Equal(1, e!.TagReadScheduleEvent!.Value.args.TagScheduleEvent.ErrorNumber);
			Assert.Equal("1", e!.TagReadScheduleEvent!.Value.args.TagScheduleEvent.Description);

			Assert.Null(queue.PopOrDefault());
		}

		[Fact]
		public void Test_aggregating_tagRead_data2_on_data1()
		{
			Model.Device device = new Model.Device(new Conf.DeviceConfiguration());
			Model.Tag t20 = new Model.Tag(new Conf.TagConfiguration("DB20", Conf.TagType.READ, 10, 100, 1));

			var driver = new MockDeviceDriver(device);

			var queue = new AggregatingConnectorEventQueue();

			Assert.Null(queue.PopOrDefault());

			byte[] data1 = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
			byte[] data2 = new byte[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };

			queue.Push(CompositeConnectorEvent.TagRead((null, new TagScheduleEventArgs(driver, TagScheduleEvent.BuildTagData(device, t20, 20, data1, false)))));
			queue.Push(CompositeConnectorEvent.TagRead((null, new TagScheduleEventArgs(driver, TagScheduleEvent.BuildTagData(device, t20, 25, data2, false)))));

			var e = queue.PopOrDefault();
			Assert.NotNull(e?.TagReadScheduleEvent);
			TagScheduleEvent te = e!.TagReadScheduleEvent!.Value.args.TagScheduleEvent;
			Assert.Equal(device, te.Device);
			Assert.Equal(t20, te.Tag);
			Assert.Equal(0, te.ErrorNumber);
			Assert.Null(te.Description);
			Assert.NotNull(te.Data);
			Assert.Equal(15, te.Data!.Length);
			Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }, te.Data);

			Assert.Null(queue.PopOrDefault());
		}

		[Fact]
		public void Test_aggregating_tagRead_data1_on_data2()
		{
			Model.Device device = new Model.Device(new Conf.DeviceConfiguration());
			Model.Tag t20 = new Model.Tag(new Conf.TagConfiguration("DB20", Conf.TagType.READ, 10, 100, 1));

			var driver = new MockDeviceDriver(device);

			var queue = new AggregatingConnectorEventQueue();

			Assert.Null(queue.PopOrDefault());

			byte[] data1 = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
			byte[] data2 = new byte[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };

			queue.Push(CompositeConnectorEvent.TagRead((null, new TagScheduleEventArgs(driver, TagScheduleEvent.BuildTagData(device, t20, 25, data1, false)))));
			queue.Push(CompositeConnectorEvent.TagRead((null, new TagScheduleEventArgs(driver, TagScheduleEvent.BuildTagData(device, t20, 20, data2, false)))));

			var e = queue.PopOrDefault();
			Assert.NotNull(e?.TagReadScheduleEvent);
			TagScheduleEvent te = e!.TagReadScheduleEvent!.Value.args.TagScheduleEvent;
			Assert.Equal(device, te.Device);
			Assert.Equal(t20, te.Tag);
			Assert.Equal(0, te.ErrorNumber);
			Assert.Null(te.Description);
			Assert.NotNull(te.Data);
			Assert.Equal(15, te.Data!.Length);
			Assert.Equal(new byte[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 6, 7, 8, 9, 10 }, te.Data);

			Assert.Null(queue.PopOrDefault());
		}

		[Fact]
		public void Test_not_aggregating_tagRead()
		{
			Model.Device device = new Model.Device(new Conf.DeviceConfiguration());
			Model.Tag t20 = new Model.Tag(new Conf.TagConfiguration("DB20", Conf.TagType.READ, 10, 100, 1));
			Model.Tag t22 = new Model.Tag(new Conf.TagConfiguration("DB22", Conf.TagType.READ, 10, 100, 1));

			var driver = new MockDeviceDriver(device);

			var queue = new AggregatingConnectorEventQueue();

			Assert.Null(queue.PopOrDefault());

			byte[] data1 = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
			byte[] data2 = new byte[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 };
			byte[] data3 = new byte[] { 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 };

			queue.Push(CompositeConnectorEvent.TagRead((null, new TagScheduleEventArgs(driver, TagScheduleEvent.BuildTagData(device, t20, 20, data1, false)))));
			queue.Push(CompositeConnectorEvent.TagRead((null, new TagScheduleEventArgs(driver, TagScheduleEvent.BuildTagData(device, t22, 25, data2, false)))));
			queue.Push(CompositeConnectorEvent.TagRead((null, new TagScheduleEventArgs(driver, TagScheduleEvent.BuildTagData(device, t20, 25, data3, false)))));

			var e = queue.PopOrDefault();
			Assert.NotNull(e?.TagReadScheduleEvent);
			TagScheduleEvent te = e!.TagReadScheduleEvent!.Value.args.TagScheduleEvent;
			Assert.Equal(device, te.Device);
			Assert.Equal(t20, te.Tag);
			Assert.Equal(0, te.ErrorNumber);
			Assert.Null(te.Description);
			Assert.NotNull(te.Data);
			Assert.Equal(10, te.Data!.Length);
			Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, te.Data);

			e = queue.PopOrDefault();
			Assert.NotNull(e?.TagReadScheduleEvent);
			te = e!.TagReadScheduleEvent!.Value.args.TagScheduleEvent;
			Assert.Equal(device, te.Device);
			Assert.Equal(t22, te.Tag);
			Assert.Equal(0, te.ErrorNumber);
			Assert.Null(te.Description);
			Assert.NotNull(te.Data);
			Assert.Equal(10, te.Data!.Length);
			Assert.Equal(new byte[] { 11, 12, 13, 14, 15, 16, 17, 18, 19, 20 }, te.Data);

			e = queue.PopOrDefault();
			Assert.NotNull(e?.TagReadScheduleEvent);
			te = e!.TagReadScheduleEvent!.Value.args.TagScheduleEvent;
			Assert.Equal(device, te.Device);
			Assert.Equal(t20, te.Tag);
			Assert.Equal(0, te.ErrorNumber);
			Assert.Null(te.Description);
			Assert.NotNull(te.Data);
			Assert.Equal(10, te.Data!.Length);
			Assert.Equal(new byte[] { 21, 22, 23, 24, 25, 26, 27, 28, 29, 30 }, te.Data);

			Assert.Null(queue.PopOrDefault());
		}
	}
}
