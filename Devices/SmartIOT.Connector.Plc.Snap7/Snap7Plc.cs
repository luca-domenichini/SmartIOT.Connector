using Sharp7;
using System.Text.RegularExpressions;

namespace SmartIOT.Connector.Plc.Snap7;

public class Snap7Plc : Core.Model.Device
{
    private static readonly Regex RegexDB = new Regex(@"^DB(?<tag>[0-9]*)$");

    public S7Client S7Client { get; init; }
    public new Snap7PlcConfiguration Configuration => (Snap7PlcConfiguration)base.Configuration;
    public bool IsConnected => S7Client.Connected;

    public Snap7Plc(Snap7PlcConfiguration plcConfiguration) : base(plcConfiguration)
    {
        S7Client = new S7Client();
    }

    public int Connect()
    {
        S7Client.SetConnectionType((ushort)Configuration.ConnectionType);
        int err = S7Client.ConnectTo(Configuration.IpAddress, Configuration.Rack, Configuration.Slot);

        if (err == 0)
        {
            int pduLength = S7Client.PduSizeNegotiated;
            if (pduLength > 0)
            {
                PDULength = pduLength;
                SinglePDUWriteBytes = pduLength - 35; // 35 bytes di header nel protocollo ISO/TCP (vedere S7Client#WriteArea())
                SinglePDUReadBytes = pduLength - 18; // 18 bytes di header nel protocollo ISO/TCP (vedere S7Client#ReadArea())
            }
        }

        return err;
    }

    public int Disconnect()
    {
        return S7Client.Disconnect();
    }

    public int ReadBytes(string tagId, int startOffset, byte[] data, int length)
    {
        if (int.TryParse(tagId, out int t))
        {
            return S7Client.DBRead(t, startOffset, length, data);
        }
        else
        {
            var match = RegexDB.Match(tagId);
            if (match.Success)
            {
                t = int.Parse(match.Groups["tag"].Value);
                return S7Client.DBRead(t, startOffset, length, data);
            }

            // other tag types can be supported here..
            throw new ArgumentException($"TagId {tagId} not handled. TagId must be in the form \"DB<number>\"");
        }
    }

    public int WriteBytes(string tagId, int startOffset, byte[] data)
    {
        if (int.TryParse(tagId, out int t))
        {
            return S7Client.DBWrite(t, startOffset, data.Length, data);
        }
        else
        {
            var match = RegexDB.Match(tagId);
            if (match.Success)
            {
                t = int.Parse(match.Groups["tag"].Value);
                return S7Client.DBWrite(t, startOffset, data.Length, data);
            }

            // other tag types can be supported here..
            throw new ArgumentException($"TagId {tagId} not handled. TagId must be in the form \"DB<number>\"");
        }
    }

    public int GetCpuInfo(S7Client.S7CpuInfo info)
    {
        return S7Client.GetCpuInfo(ref info);
    }

    public int GetOrderCode(S7Client.S7OrderCode oc)
    {
        return S7Client.GetOrderCode(ref oc);
    }
}
