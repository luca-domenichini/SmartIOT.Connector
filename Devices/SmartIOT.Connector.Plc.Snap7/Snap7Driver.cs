using Sharp7;
using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Model;
using System.Text;
using static Sharp7.S7Client;

namespace SmartIOT.Connector.Plc.Snap7;

public class Snap7Driver : IDeviceDriver
{
    public string Name => $"{nameof(Snap7Driver)}.{Device.Name}";
    public Device Device { get; }

    public Snap7Driver(Snap7Plc plc)
    {
        Device = plc;
    }

    public int StartInterface()
    {
        return 0;
    }

    public int StopInterface()
    {
        return 0;
    }

    public int Connect(Device device)
    {
        lock (device)
        {
            Snap7Plc p = (Snap7Plc)device;
            return p.Connect();
        }
    }

    public int Disconnect(Device device)
    {
        lock (device)
        {
            Snap7Plc p = (Snap7Plc)device;
            return p.Disconnect();
        }
    }

    public int ReadTag(Device device, Tag tag, byte[] data, int startOffset, int length)
    {
        Snap7Plc p = (Snap7Plc)device;

        var bytes = new byte[length];

        int ret = p.ReadBytes(tag.TagId, startOffset, bytes, length);
        if (ret != 0)
            return ret;

        Array.Copy(bytes, 0, data, startOffset - tag.ByteOffset, length);

        return 0;
    }

    public int WriteTag(Device device, Tag tag, byte[] data, int startOffset, int length)
    {
        byte[] bytes = new byte[length];
        Array.Copy(data, startOffset - tag.ByteOffset, bytes, 0, length);

        Snap7Plc p = (Snap7Plc)device;

        return p.WriteBytes(tag.TagId, startOffset, bytes);
    }

    public string GetErrorMessage(int errorNumber)
    {
        return S7Client.ErrorText(errorNumber);
    }

    public string GetDeviceDescription(Device device)
    {
        lock (device)
        {
            Snap7Plc p = (Snap7Plc)device;
            if (p.IsConnected)
            {
                StringBuilder sb = new StringBuilder();

                S7OrderCode oc = new S7OrderCode();
                S7CpuInfo info = new S7CpuInfo();

                int ret = p.GetCpuInfo(info);
                if (ret == 0)
                {
                    sb.Append(info.ASName?.Trim()).Append(" - ");
                }

                ret = p.GetOrderCode(oc);
                if (ret == 0)
                {
                    sb.Append(oc.Code);
                }

                return sb.ToString();
            }
            else
                return "PLC not connected";
        }
    }
}
