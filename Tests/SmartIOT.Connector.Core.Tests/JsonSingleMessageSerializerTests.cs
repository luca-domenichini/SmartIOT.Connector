using SmartIOT.Connector.Messages;
using SmartIOT.Connector.Messages.Serializers;
using Xunit;

namespace SmartIOT.Connector.Core.Tests;

public class JsonSingleMessageSerializerTests
{
    [Fact]
    public void Test_serializer_tagEvent()
    {
        TagEvent e = TagEvent.CreateTagDataEvent("1", "DB20", 10, new byte[] { 1, 2, 3, 4, 5 });

        var serializer = new JsonSingleMessageSerializer();
        var bytes = serializer.SerializeMessage(e);

        var e2 = serializer.DeserializeMessage<TagEvent>(bytes);

        Assert.NotNull(e2);
        Assert.Equal("1", e2!.DeviceId);
        Assert.Equal("DB20", e2.TagId);
        Assert.Equal(10, e2.StartOffset);
        Assert.Equal(5, e2.Data!.Length);
        Assert.Equal(1, e2.Data[0]);
        Assert.Equal(2, e2.Data[1]);
        Assert.Equal(3, e2.Data[2]);
        Assert.Equal(4, e2.Data[3]);
        Assert.Equal(5, e2.Data[4]);
    }

    [Fact]
    public void Test_serializer_DeviceEvent()
    {
        DeviceEvent e = new DeviceEvent("1", DeviceStatus.OK, 10, "Test");

        var serializer = new JsonSingleMessageSerializer();
        var bytes = serializer.SerializeMessage(e);

        var e2 = serializer.DeserializeMessage<DeviceEvent>(bytes);

        Assert.NotNull(e2);
        Assert.Equal("1", e2!.DeviceId);
        Assert.Equal(DeviceStatus.OK, e2.DeviceStatus);
        Assert.Equal(10, e2.ErrorNumber);
        Assert.Equal("Test", e2.Description);
    }
}
