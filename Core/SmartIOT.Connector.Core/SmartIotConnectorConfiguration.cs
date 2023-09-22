using SmartIOT.Connector.Core.Conf;
using System.Text.Json;

namespace SmartIOT.Connector.Core;

public class SmartIotConnectorConfiguration
{
    public IList<string> ConnectorConnectionStrings { get; set; } = new List<string>();
    public IList<DeviceConfiguration> DeviceConfigurations { get; set; } = new List<DeviceConfiguration>();
    public SchedulerConfiguration SchedulerConfiguration { get; set; } = new SchedulerConfiguration();

    public static SmartIotConnectorConfiguration? FromJson(string json)
    {
        return JsonSerializer.Deserialize<SmartIotConnectorConfiguration>(json, new JsonSerializerOptions()
        {
            ReadCommentHandling = JsonCommentHandling.Skip
        });
    }
}
