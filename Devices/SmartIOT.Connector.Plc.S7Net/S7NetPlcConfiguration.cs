using S7.Net;
using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Util;

namespace SmartIOT.Connector.Plc.S7Net
{
    public class S7NetPlcConfiguration : DeviceConfiguration
    {
        internal CpuType CpuType { get; init; }
        internal string IpAddress { get; init; }
        internal int? Port { get; init; }
        internal short Rack { get; init; }
        internal short Slot { get; init; }

        public S7NetPlcConfiguration(DeviceConfiguration configuration) : base(configuration)
        {
            var tokens = ConnectionStringParser.ParseTokens(configuration.ConnectionString);

            IpAddress = tokens.GetOrDefault("ip")!; // suppressing null-warning: check is next line
            if (string.IsNullOrWhiteSpace(IpAddress))
                throw new ArgumentException("Ip not found in ConnectionString. Provide a valid IpAddress or HostName");

            var sPort = tokens.GetOrDefault("port");
            if (!string.IsNullOrWhiteSpace(sPort) && int.TryParse(sPort, out var port))
                Port = port;

            var cpu = tokens.GetOrDefault("cputype");
            if (string.IsNullOrWhiteSpace(cpu))
                throw new ArgumentException($"CpuType not found in ConnectionString. Valid values are [{GetEnumValues()}]");
            if (!Enum.TryParse(cpu, out CpuType cpuType))
                throw new ArgumentException($"CpuType {cpu} not valid in ConnectionString. Valid values are [{GetEnumValues()}]");
            CpuType = cpuType;

            var sRack = tokens.GetOrDefault("rack") ?? "0";
            if (!short.TryParse(sRack, out var rack))
                throw new ArgumentException($"Rack not valid in ConnectionString. Provide a valid number");
            Rack = rack;

            var sSlot = tokens.GetOrDefault("slot") ?? "0";
            if (!short.TryParse(sSlot, out var slot))
                throw new ArgumentException($"Slot not valid in ConnectionString. Provide a valid number.");
            Slot = slot;
        }

        private string GetEnumValues()
        {
            return string.Join(", ", Enum.GetValues<CpuType>().Select(Enum.GetName));
        }
    }
}
