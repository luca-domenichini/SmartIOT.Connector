using System.Text.RegularExpressions;

namespace SmartIOT.Connector.Plc.S7Net
{
    public class S7NetPlc : Core.Model.Device
    {
        private static readonly Regex RegexDB = new Regex(@"^DB(?<tag>[0-9]*)$");

        private readonly S7.Net.Plc _plc;

        public new S7NetPlcConfiguration Configuration => (S7NetPlcConfiguration)base.Configuration;

        public S7NetPlc(S7NetPlcConfiguration deviceConfiguration) : base(deviceConfiguration)
        {
            if (deviceConfiguration.Port != null)
                _plc = new S7.Net.Plc(deviceConfiguration.CpuType, deviceConfiguration.IpAddress, deviceConfiguration.Port.Value, deviceConfiguration.Rack, deviceConfiguration.Slot);
            else
                _plc = new S7.Net.Plc(deviceConfiguration.CpuType, deviceConfiguration.IpAddress, deviceConfiguration.Rack, deviceConfiguration.Slot);
        }

        public void Connect()
        {
            _plc.Open();

            int pduLength = _plc.MaxPDUSize;
            if (pduLength > 0)
            {
                PDULength = pduLength;
                SinglePDUWriteBytes = pduLength - 35; // 35 bytes di header nel protocollo ISO/TCP (vedere S7Client#WriteArea())
                SinglePDUReadBytes = pduLength - 18; // 18 bytes di header nel protocollo ISO/TCP (vedere S7Client#ReadArea())
            }
        }

        public void Disconnect()
        {
            _plc.Close();
        }

        public byte[] ReadBytes(string tagId, int startOffset, int length)
        {
            if (int.TryParse(tagId, out int tag))
            {
                return _plc.ReadBytes(S7.Net.DataType.DataBlock, tag, startOffset, length);
            }
            else
            {
                var match = RegexDB.Match(tagId);
                if (match.Success)
                {
                    tag = int.Parse(match.Groups["tag"].Value);
                    return _plc.ReadBytes(S7.Net.DataType.DataBlock, tag, startOffset, length);
                }

                // other tag types can be supported here..
                throw new ArgumentException($"TagId {tagId} not handled. TagId must be in the form \"DB<number>\"", nameof(tagId));
            }
        }

        public void WriteBytes(string tagId, int startOffset, byte[] bytes)
        {
            if (int.TryParse(tagId, out int tag))
            {
                _plc.WriteBytes(S7.Net.DataType.DataBlock, tag, startOffset, bytes);
            }
            else
            {
                var match = RegexDB.Match(tagId);
                if (match.Success)
                {
                    tag = int.Parse(match.Groups["tag"].Value);
                    _plc.WriteBytes(S7.Net.DataType.DataBlock, tag, startOffset, bytes);
                }

                // other tag types can be supported here..
                throw new ArgumentException($"TagId {tagId} not handled. TagId must be in the form \"DB<number>\"", nameof(tagId));
            }
        }
    }
}
