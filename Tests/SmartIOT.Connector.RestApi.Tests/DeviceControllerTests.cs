using Microsoft.AspNetCore.Mvc;
using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.RestApi.Controllers.V1;
using SmartIOT.Connector.RestApi.Model;
using SmartIOT.Connector.RestApi.Services;

namespace SmartIOT.Connector.RestApi.Tests;

public class DeviceControllerTests
{
    private DeviceController SetupController()
    {
        var builder = new SmartIotConnectorBuilder()
            .WithAutoDiscoverConnectorFactories()
            .WithAutoDiscoverDeviceDriverFactories()
            .WithConfigurationJsonFilePath("test-config.json");

        var sic = builder.Build();

        DeviceService service = new DeviceService(sic, builder.SchedulerFactory, builder.DeviceDriverFactory, builder.TimeService);

        return new DeviceController(sic, service);
    }

    [Fact]
    public void Test_GetConfiguration()
    {
        var controller = SetupController();

        OkObjectResult r = (OkObjectResult)controller.GetConfiguration();

        IList<DeviceConfiguration> list = (IList<DeviceConfiguration>)r.Value!;

        Assert.Single(list);

        var c = list[0];

        Assert.Equal("1", c.DeviceId);
    }

    [Fact]
    public void Test_GetDevice()
    {
        var controller = SetupController();

        OkObjectResult r = (OkObjectResult)controller.GetDevice("1");

        Device d = (Device)r.Value!;

        Assert.Equal("1", d.DeviceId);
        Assert.Equal(Core.Model.DeviceStatus.UNINITIALIZED, d.DeviceStatus);
    }

    [Fact]
    public void Test_GetDeviceConfiguration()
    {
        var controller = SetupController();

        OkObjectResult r = (OkObjectResult)controller.GetDeviceConfiguration("1");

        DeviceConfiguration d = (DeviceConfiguration)r.Value!;

        Assert.Equal("1", d.DeviceId);
        Assert.True(d.Enabled);
    }

    [Fact]
    public void Test_GetDevices()
    {
        var controller = SetupController();

        OkObjectResult r = (OkObjectResult)controller.GetDevices();

        IList<Device> list = (IList<Device>)r.Value!;

        Assert.Single(list);

        var d = list[0];

        Assert.Equal("1", d.DeviceId);
        Assert.Equal(Core.Model.DeviceStatus.UNINITIALIZED, d.DeviceStatus);
    }

    [Fact]
    public void Test_GetTag()
    {
        var controller = SetupController();

        OkObjectResult r = (OkObjectResult)controller.GetTag("1", "DB20");

        Tag tag = (Tag)r.Value!;

        Assert.Equal("DB20", tag.TagId);
        Assert.Equal(TagType.READ, tag.TagType);
    }

    [Fact]
    public void Test_GetTagConfiguration()
    {
        var controller = SetupController();

        OkObjectResult r = (OkObjectResult)controller.GetTagConfiguration("1", "DB20");

        TagConfiguration tag = (TagConfiguration)r.Value!;

        Assert.Equal("DB20", tag.TagId);
        Assert.Equal(TagType.READ, tag.TagType);
    }

    [Fact]
    public void Test_GetTagData()
    {
        var controller = SetupController();

        OkObjectResult r = (OkObjectResult)controller.GetTagData("1", "DB20");

        TagData data = (TagData)r.Value!;

        Assert.Equal(0, data.StartOffset);
        Assert.Equal(100, data.Bytes.Length);
    }

    [Fact]
    public void Test_SetTagData()
    {
        var controller = SetupController();

        OkObjectResult r = (OkObjectResult)controller.GetTagData("1", "DB20");
        TagData data = (TagData)r.Value!;

        Assert.Equal(0, data.StartOffset);
        Assert.Equal(100, data.Bytes.Length);

        Assert.IsType<OkResult>(controller.SetTagData("1", "DB20", new TagData(10, new byte[] { 1, 2, 3 })));

        r = (OkObjectResult)controller.GetTagData("1", "DB20");
        data = (TagData)r.Value!;

        Assert.Equal(0, data.StartOffset);
        Assert.Equal(100, data.Bytes.Length);
        Assert.Equal(1, data.Bytes[10]);
        Assert.Equal(2, data.Bytes[11]);
        Assert.Equal(3, data.Bytes[12]);
    }

    [Fact]
    public void Test_EnableDevice()
    {
        var controller = SetupController();

        OkObjectResult r = (OkObjectResult)controller.IsDeviceEnabled("1");
        Assert.True((bool)r.Value!);

        Assert.IsType<OkResult>(controller.SetDeviceEnabled("1", false));

        r = (OkObjectResult)controller.IsDeviceEnabled("1");
        Assert.False((bool)r.Value!);
    }

    [Fact]
    public void Test_AddRemoveUpdate_Device_and_Tag()
    {
        var controller = SetupController();

        Assert.IsType<OkResult>(controller.AddDevice(new DeviceConfiguration("mock://", "2", true, "D2", new List<TagConfiguration>()
        {
            new TagConfiguration("DB100", TagType.READ, 0, 10, 1)
        })));

        OkObjectResult r = (OkObjectResult)controller.GetDeviceConfiguration("2");
        DeviceConfiguration d = (DeviceConfiguration)r.Value!;

        Assert.Equal("2", d.DeviceId);
        Assert.Single(d.Tags);
        Assert.Equal("DB100", d.Tags[0].TagId);

        Assert.IsType<OkResult>(controller.AddTag("2", new TagConfiguration("DB200", TagType.WRITE, 0, 10, 1)));

        r = (OkObjectResult)controller.GetDeviceConfiguration("2");
        d = (DeviceConfiguration)r.Value!;

        Assert.Equal("2", d.DeviceId);
        Assert.Equal(2, d.Tags.Count);
        Assert.Equal("DB200", d.Tags[1].TagId);

        Assert.IsType<OkResult>(controller.RemoveTag("2", "DB100"));

        r = (OkObjectResult)controller.GetDeviceConfiguration("2");
        d = (DeviceConfiguration)r.Value!;

        Assert.Equal("2", d.DeviceId);
        Assert.Single(d.Tags);
        Assert.Equal("DB200", d.Tags[0].TagId);

        Assert.IsType<OkResult>(controller.UpdateTag("2", new TagConfiguration("DB200", TagType.READ, 10, 100, 2)));

        r = (OkObjectResult)controller.GetDeviceConfiguration("2");
        d = (DeviceConfiguration)r.Value!;

        Assert.Equal("2", d.DeviceId);
        Assert.Single(d.Tags);
        Assert.Equal("DB200", d.Tags[0].TagId);

        Assert.Equal(TagType.READ, d.Tags[0].TagType);
        Assert.Equal(10, d.Tags[0].ByteOffset);
        Assert.Equal(100, d.Tags[0].Size);
        Assert.Equal(2, d.Tags[0].Weight);

        Assert.IsType<OkResult>(controller.RemoveDevice("2"));

        Assert.IsType<NotFoundResult>(controller.GetDeviceConfiguration("2"));
    }
}
