using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace SmartIOT.Connector.Prometheus.Tests
{
    public class TestMetrics
    {
        [Fact]
        public async Task Test_prometheus_metrics()
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

            await smartiot.StartAsync();
            try
            {
                Assert.True(started.WaitOne(TimeSpan.FromSeconds(2)));

                await Task.Delay(1000);

                var http = new HttpClient();
                var message = await http.GetAsync("http://localhost:9001/metrics");

                Assert.Equal(System.Net.HttpStatusCode.OK, message.StatusCode);
                Assert.Contains("smartiot_connector_synchronization_count", await message.Content.ReadAsStringAsync());

                await smartiot.StopAsync();

                Assert.True(stopped.WaitOne(TimeSpan.FromSeconds(2)));
            }
            finally
            {
                await smartiot.StopAsync();
            }
        }
    }
}