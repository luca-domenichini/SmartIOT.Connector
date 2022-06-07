using System;
using System.Net.Http;
using System.Threading;
using Xunit;

namespace SmartIOT.Connector.Prometheus.Tests
{
	public class Test_metrics
	{
		[Fact]
		public void Test_prometheus_metrics()
		{
			var smartiot = new SmartIOT.Connector.Core.SmartIotConnectorBuilder()
				.WithAutoDiscoverDeviceDriverFactories()
				.WithAutoDiscoverConnectorFactories()
				.WithConfigurationJsonFilePath("test-config.json")
				.Build()
				.AddManagedPrometheusServer("localhost", 9001);

			var started = new AutoResetEvent(false);
			var stopped = new AutoResetEvent(false);

			smartiot.Started += (s, e) => started.Set();
			smartiot.Stopped += (s, e) => stopped.Set();

			smartiot.Start();
			try
			{
				Assert.True(started.WaitOne(TimeSpan.FromSeconds(2)));

				Thread.Sleep(1000);

				var http = new HttpClient();
				var message = http.GetAsync("http://localhost:9001/metrics").Result;

				Assert.Equal(System.Net.HttpStatusCode.OK, message.StatusCode);
				Assert.Contains("smartiot_connector_synchronization_count", message.Content.ReadAsStringAsync().Result);

				smartiot.Stop();

				Assert.True(stopped.WaitOne(TimeSpan.FromSeconds(2)));
			}
			finally
			{
				smartiot.Stop();
			}
		}
	}
}