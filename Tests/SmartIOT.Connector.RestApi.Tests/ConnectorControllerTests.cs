using Microsoft.AspNetCore.Mvc;
using SmartIOT.Connector.Core;
using SmartIOT.Connector.Mocks;
using SmartIOT.Connector.RestApi.Controllers.V1;
using SmartIOT.Connector.RestApi.Services;

namespace SmartIOT.Connector.RestApi.Tests
{
	public class ConnectorControllerTests
	{
		private ConnectorController SetupController()
		{
			var builder = new SmartIotConnectorBuilder()
				.WithAutoDiscoverConnectorFactories()
				.WithAutoDiscoverDeviceDriverFactories()
				.WithConfigurationJsonFilePath("test-config.json");

			var sic = builder.Build();

			var service = new ConnectorService(sic, builder.ConnectorFactory);

			return new ConnectorController(sic, service);
		}

		[Fact]
		public void Test_GetConnectors()
		{
			var controller = SetupController();

			var list = controller.GetConnectors();

			Assert.Single(list);

			var c = list[0];

			Assert.Equal(0, c.Index);
			Assert.Equal("fake://", c.ConnectionString);
		}
		
		[Fact]
		public void Test_GetConnector_ok()
		{
			var controller = SetupController();

			var r = controller.GetConnector(0);

			OkObjectResult ok = (OkObjectResult)r;

			Model.Connector c = (Model.Connector)ok.Value!;

			Assert.Equal(0, c.Index);
			Assert.Equal("fake://", c.ConnectionString);
		}
		[Fact]
		public void Test_GetConnector_notfound()
		{
			var controller = SetupController();

			var r = controller.GetConnector(1);

			Assert.IsType<NotFoundResult>(r);
		}
		
		[Fact]
		public void Test_AddConnector()
		{
			var controller = SetupController();

			var r = controller.AddConnector("mock://");

			OkObjectResult ok = (OkObjectResult)r;

			Model.Connector c = (Model.Connector)ok.Value!;

			Assert.Equal(1, c.Index);
			Assert.Equal("mock://", c.ConnectionString);
		}

		[Fact]
		public void Test_UpdateConnector()
		{
			var controller = SetupController();

			var r = controller.UpdateConnector(0, "mock://");

			OkObjectResult ok = (OkObjectResult)r;

			Model.Connector c = (Model.Connector)ok.Value!;

			Assert.Equal(0, c.Index);
			Assert.Equal("mock://", c.ConnectionString);
		}
		
		[Fact]
		public void Test_RemoveConnector()
		{
			var controller = SetupController();

			var r = controller.RemoveConnector(0);

			Assert.IsType<OkResult>(r);
			Assert.Empty(controller.GetConnectors());
		}

	}
}