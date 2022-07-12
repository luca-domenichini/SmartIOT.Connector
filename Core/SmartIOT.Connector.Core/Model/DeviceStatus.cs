using System.Text.Json.Serialization;

namespace SmartIOT.Connector.Core.Model
{
	[JsonConverter(typeof(JsonStringEnumConverter))]
	public enum DeviceStatus
	{
		UNINITIALIZED,
		OK,
		ERROR,
		DISABLED,
	}
}
