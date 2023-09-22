using SmartIOT.Connector.Messages;
using SmartIOT.Connector.Messages.Serializers;
using System;
using System.IO;
using System.Text;
using Xunit;

namespace SmartIOT.Connector.Core.Tests;

public class JsonStreamMessageSerializerTests
{
    [Fact]
    public void Test_serializer()
    {
        TagEvent e = TagEvent.CreateTagDataEvent("1", "DB20", 0, new byte[] { 1, 2, 3, 4, 5 });
        DeviceEvent d = new DeviceEvent("1", DeviceStatus.OK, 0, "Test");

        var serializer = new JsonStreamMessageSerializer();

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

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Asadsda(bool insertMessageType)
    {
        MemoryStream s = new MemoryStream();
        byte[] b1 = Encoding.UTF8.GetBytes("{\"DeviceId\":\"1\",\"TagId\":\"DB20\",\"StartOffset\":0,\"Data\":\"AQIDBAU = \",\"IsInitializationEvent\":false,\"ErrorNumber\":0,\"Description\":null}\r\n");
        byte[] b2 = Encoding.UTF8.GetBytes("{\"DeviceId\":\"1\",\"DeviceStatus\":1,\"ErrorNumber\":0,\"Description\":\"Test\"}\r\n");

        int pad = insertMessageType ? 1 : 0;
        byte[] data = new byte[pad + b1.Length + pad + b2.Length];
        if (insertMessageType)
            data[0] = 1;
        Array.Copy(b1, 0, data, pad, b1.Length);
        if (insertMessageType)
            data[pad + b1.Length] = 2;
        Array.Copy(b2, 0, data, pad + b1.Length + pad, b2.Length);

        s.Write(data);

        s.Position = 0;

        using (StreamReader r = new StreamReader(s, Encoding.UTF8, false, -1, true))
        {
            if (insertMessageType)
                Assert.Equal(1, r.Read());
            Assert.Equal("{\"DeviceId\":\"1\",\"TagId\":\"DB20\",\"StartOffset\":0,\"Data\":\"AQIDBAU = \",\"IsInitializationEvent\":false,\"ErrorNumber\":0,\"Description\":null}", r.ReadLine());

            if (insertMessageType)
                Assert.Equal(2, r.Read());
            Assert.Equal("{\"DeviceId\":\"1\",\"DeviceStatus\":1,\"ErrorNumber\":0,\"Description\":\"Test\"}", r.ReadLine());

            Assert.Null(r.ReadLine());
        }
    }

    [Fact]
    public void Asadsda2()
    {
        FileStream s = new FileStream("protobuf.txt", FileMode.Open, FileAccess.Read);

        using (StreamReader r = new StreamReader(s, Encoding.UTF8, false, -1, true))
        {
            Assert.Equal(49, r.Read());
            Assert.Equal("{\"DeviceId\":\"1\",\"TagId\":\"DB20\",\"StartOffset\":0,\"Data\":\"AQIDBAU = \",\"IsInitializationEvent\":false,\"ErrorNumber\":0,\"Description\":null}", r.ReadLine());

            Assert.Equal(50, r.Read());
            Assert.Equal("{\"DeviceId\":\"1\",\"DeviceStatus\":1,\"ErrorNumber\":0,\"Description\":\"Test\"}", r.ReadLine());

            Assert.Null(r.ReadLine());
        }
    }
}
