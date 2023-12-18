using Microsoft.AspNetCore.Mvc;
using SmartIOT.Connector.Core;
using SmartIOT.Connector.Mocks;
using SmartIOT.Connector.RestApi.Controllers.V1;
using SmartIOT.Connector.RestApi.Services;

namespace SmartIOT.Connector.RestApi.Tests;

public class ConnectorControllerTests
{
    private static SmartIotConnectorBuilder CreateBuilder()
    {
        return new SmartIotConnectorBuilder()
            .WithAutoDiscoverConnectorFactories()
            .WithAutoDiscoverDeviceDriverFactories()
            .WithConfigurationJsonFilePath("test-config.json");
    }

    private ConnectorController SetupController(SmartIotConnector sic, SmartIotConnectorBuilder builder)
    {
        var service = new ConnectorService(sic, builder.ConnectorFactory);

        return new ConnectorController(sic, service);
    }

    [Fact]
    public void Test_GetConnectors()
    {
        SmartIotConnectorBuilder builder = CreateBuilder();
        SmartIotConnector sic = builder.Build();
        var controller = SetupController(sic, builder);

        var list = controller.GetConnectors();

        Assert.Single(list);

        var c = list[0];

        Assert.Equal(0, c.Index);
        Assert.Equal("fake://", c.ConnectionString);
    }

    [Fact]
    public void Test_GetConnector_ok()
    {
        SmartIotConnectorBuilder builder = CreateBuilder();
        SmartIotConnector sic = builder.Build();
        var controller = SetupController(sic, builder);

        var r = controller.GetConnector(0);

        OkObjectResult ok = (OkObjectResult)r;

        Model.Connector c = (Model.Connector)ok.Value!;

        Assert.Equal(0, c.Index);
        Assert.Equal("fake://", c.ConnectionString);
    }

    [Fact]
    public void Test_GetConnector_notfound()
    {
        SmartIotConnectorBuilder builder = CreateBuilder();
        SmartIotConnector sic = builder.Build();
        var controller = SetupController(sic, builder);

        var r = controller.GetConnector(1);

        Assert.IsType<NotFoundResult>(r);
    }

    [Fact]
    public async Task Test_AddConnector()
    {
        SmartIotConnectorBuilder builder = CreateBuilder();
        SmartIotConnector sic = builder.Build();
        var controller = SetupController(sic, builder);

        var r = await controller.AddConnector("mock://");

        OkObjectResult ok = (OkObjectResult)r;

        Model.Connector c = (Model.Connector)ok.Value!;

        Assert.Equal(1, c.Index);
        Assert.Equal("mock://", c.ConnectionString);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Test_UpdateConnector(bool startConnector)
    {
        SmartIotConnectorBuilder builder = CreateBuilder();
        SmartIotConnector sic = builder.Build();
        var controller = SetupController(sic, builder);

        FakeConnector connector = (FakeConnector)sic.Connectors[0];

        if (startConnector)
            await sic.StartAsync();

        var r = await controller.UpdateConnector(0, "mock://");

        OkObjectResult ok = (OkObjectResult)r;

        Model.Connector c = (Model.Connector)ok.Value!;

        Assert.Equal(0, c.Index);
        Assert.Equal("mock://", c.ConnectionString);

        if (startConnector)
            Assert.True(connector.StopEvent.IsSet);
        else
            Assert.False(connector.StopEvent.IsSet);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Test_RemoveConnector(bool startConnector)
    {
        SmartIotConnectorBuilder builder = CreateBuilder();
        SmartIotConnector sic = builder.Build();
        var controller = SetupController(sic, builder);

        FakeConnector connector = (FakeConnector)sic.Connectors[0];

        if (startConnector)
            await sic.StartAsync();

        var r = await controller.RemoveConnector(0);

        Assert.IsType<OkResult>(r);
        Assert.Empty(controller.GetConnectors());

        if (startConnector)
            Assert.True(connector.StopEvent.IsSet);
        else
            Assert.False(connector.StopEvent.IsSet);
    }
}