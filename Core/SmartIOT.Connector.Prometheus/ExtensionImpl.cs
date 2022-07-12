using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Events;
using Prometheus;

namespace SmartIOT.Connector.Prometheus
{
	internal class ExtensionImpl
	{
		private readonly bool _isManagedServer;
		private readonly string _metricsPrefix;
		private readonly Gauge _synchronizationAvgTimeSeconds;
		private readonly Gauge _synchronizationCount;
		private readonly Gauge _writesCount;

		public IMetricServer MetricServer { get; }

		public ExtensionImpl(IMetricServer metricServer, bool isManagedServer, string metricsPrefix)
		{
			_isManagedServer = isManagedServer;
			_metricsPrefix = metricsPrefix;

			if (string.IsNullOrWhiteSpace(_metricsPrefix))
				_metricsPrefix = "smartiot_connector_";
			if (!_metricsPrefix.EndsWith("_"))
				_metricsPrefix += "_";

			MetricServer = metricServer;

			_synchronizationAvgTimeSeconds = Metrics.CreateGauge($"{_metricsPrefix}synchronization_avg_time_seconds", "This metric represents the average time elapsed to synchronize a tag", new GaugeConfiguration()
			{
				LabelNames = new[] { "DeviceId", "TagId" },
				SuppressInitialValue = true,
			});
			_synchronizationCount = Metrics.CreateGauge($"{_metricsPrefix}synchronization_count", "This metric represents the number of synchronization on the tag", new GaugeConfiguration()
			{
				LabelNames = new[] { "DeviceId", "TagId" },
				SuppressInitialValue = true,
			});
			_writesCount = Metrics.CreateGauge($"{_metricsPrefix}writes_count", "This metric represents the number of writes on the tag", new GaugeConfiguration()
			{
				LabelNames = new[] { "DeviceId", "TagId" },
				SuppressInitialValue = true,
			});
		}

		internal void OnStarting(SmartIotConnector connector)
		{
			if (_isManagedServer)
				MetricServer.Start();

			connector.TagReadEvent += OnTagReadEvent;
			connector.TagWriteEvent += OnTagWriteEvent;
		}

		internal void OnStopping(SmartIotConnector connector)
		{
			connector.TagReadEvent -= OnTagReadEvent;
			connector.TagWriteEvent -= OnTagWriteEvent;

			if (_isManagedServer)
				MetricServer.Stop();
		}

		private void OnTagWriteEvent(object? sender, TagScheduleEventArgs e)
		{
			_writesCount.WithLabels($"{e.TagScheduleEvent.Device.DeviceId}", $"{e.TagScheduleEvent.Tag.TagId}")
				.Set(e.TagScheduleEvent.Tag.WritesCount);

			_synchronizationAvgTimeSeconds.WithLabels($"{e.TagScheduleEvent.Device.DeviceId}", $"{e.TagScheduleEvent.Tag.TagId}")
				.Set(e.TagScheduleEvent.Tag.SynchronizationAvgTime.TotalSeconds);

			_synchronizationCount.WithLabels($"{e.TagScheduleEvent.Device.DeviceId}", $"{e.TagScheduleEvent.Tag.TagId}")
				.Set(e.TagScheduleEvent.Tag.SynchronizationCount);
		}

		private void OnTagReadEvent(object? sender, TagScheduleEventArgs e)
		{
			_synchronizationAvgTimeSeconds.WithLabels($"{e.TagScheduleEvent.Device.DeviceId}", $"{e.TagScheduleEvent.Tag.TagId}")
				.Set(e.TagScheduleEvent.Tag.SynchronizationAvgTime.TotalSeconds);

			_synchronizationCount.WithLabels($"{e.TagScheduleEvent.Device.DeviceId}", $"{e.TagScheduleEvent.Tag.TagId}")
				.Set(e.TagScheduleEvent.Tag.SynchronizationCount);
		}

	}
}
