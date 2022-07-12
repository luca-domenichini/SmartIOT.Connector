using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Util;

namespace SmartIOT.Connector.Plc.Snap7
{
	public class Snap7PlcConfiguration : DeviceConfiguration
	{
		internal string IpAddress { get; init; }
		internal short Rack { get; init; }
		internal short Slot { get; init; }
		internal S7ConnectionType ConnectionType { get; init; }

		public Snap7PlcConfiguration(DeviceConfiguration configuration) : base(configuration)
		{
			var tokens = ConnectionStringParser.ParseTokens(configuration.ConnectionString);

			IpAddress = tokens.GetOrDefault("ip")!; // suppressing null-warning: check is next line
			if (string.IsNullOrWhiteSpace(IpAddress))
				throw new ArgumentException("Ip not found in ConnectionString. Provide a valid IpAddress or HostName");

			var sRack = tokens.GetOrDefault("rack") ?? "0";
			if (!short.TryParse(sRack, out var rack))
				throw new ArgumentException($"Rack not valid in ConnectionString. Provide a valid number");
			Rack = rack;

			var sSlot = tokens.GetOrDefault("slot") ?? "0";
			if (!short.TryParse(sSlot, out var slot))
				throw new ArgumentException($"Slot not valid in ConnectionString. Provide a valid number.");
			Slot = slot;

			var sConnectionType = tokens.GetOrDefault("type");
			if (!string.IsNullOrWhiteSpace(sConnectionType) && Enum.TryParse(sConnectionType, out S7ConnectionType connectionType))
				ConnectionType = connectionType;
			else
				ConnectionType = S7ConnectionType.BASIC;
		}
	}
}
