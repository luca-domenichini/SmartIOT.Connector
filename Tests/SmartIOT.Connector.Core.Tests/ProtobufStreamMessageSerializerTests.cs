using SmartIOT.Connector.Messages;
using SmartIOT.Connector.Messages.Serializers;
using System.IO;
using Xunit;

namespace SmartIOT.Connector.Core.Tests;

public class ProtobufStreamMessageSerializerTests
{
    [Fact]
    public void Test_serializer()
    {
        TagEvent e = TagEvent.CreateTagDataEvent("1", "DB20", 0, new byte[] { 1, 2, 3, 4, 5 });
        DeviceEvent d = new DeviceEvent("1", DeviceStatus.OK, 0, "Test");

        var serializer = new ProtobufStreamMessageSerializer();

        var stream = new MemoryStream();

        serializer.SerializeMessage(stream, e);
        serializer.SerializeMessage(stream, d);

        stream.Position = 0;

        TagEvent? e2 = (TagEvent?)serializer.DeserializeMessage(stream);
        DeviceEvent? d2 = (DeviceEvent?)serializer.DeserializeMessage(stream);

        Assert.NotNull(e2);
        Assert.Equal(e.DeviceId, e2!.DeviceId);
        Assert.Equal(e.TagId, e2.TagId);
        Assert.Equal(e.StartOffset, e2.StartOffset);
        Assert.Equal(e.Data, e2.Data);
        Assert.Equal(e.Description, e2.Description);
        Assert.Equal(e.IsInitializationEvent, e2.IsInitializationEvent);

        Assert.NotNull(d2);
        Assert.Equal(d.DeviceId, d2!.DeviceId);
        Assert.Equal(d.Description, d2.Description);
        Assert.Equal(d.DeviceStatus, d2.DeviceStatus);
        Assert.Equal(d.ErrorNumber, d2.ErrorNumber);
    }
}
