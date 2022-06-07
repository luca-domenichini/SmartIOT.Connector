using System.Text.Json;

namespace SmartIOT.Connector.Core.Conf
{
	public class DeviceConfiguration
	{
		public string ConnectionString { get; set; } = string.Empty;
		public string DeviceId { get; set; } = string.Empty;
		public bool Enabled { get; set; }
		public string Name { get; set; } = string.Empty;
		public bool IsPartialReadsEnabled { get; set; }
		public bool IsWriteOptimizationEnabled { get; set; } = true;
		public IList<TagConfiguration> Tags { get; set; } = new List<TagConfiguration>();


		public static DeviceConfiguration? FromJson(string json)
		{
			return JsonSerializer.Deserialize<DeviceConfiguration>(json, new JsonSerializerOptions()
			{
				ReadCommentHandling = JsonCommentHandling.Skip
			});
		}

		public DeviceConfiguration()
		{

		}

		public DeviceConfiguration(DeviceConfiguration configuration)
		{
			ConnectionString = configuration.ConnectionString;
			DeviceId = configuration.DeviceId;
			Enabled = configuration.Enabled;
			Name = configuration.Name;
			IsPartialReadsEnabled = configuration.IsPartialReadsEnabled;
			IsWriteOptimizationEnabled = configuration.IsWriteOptimizationEnabled;
			Tags = configuration.Tags.Select(x => new TagConfiguration(x)).ToList();
		}

		public DeviceConfiguration(string connectionString, string deviceId, bool enabled, string name, IList<TagConfiguration> tags)
		{
			ConnectionString = connectionString;
			DeviceId = deviceId;
			Enabled = enabled;
			Tags = tags;
			Name = name;
		}

	}
}
