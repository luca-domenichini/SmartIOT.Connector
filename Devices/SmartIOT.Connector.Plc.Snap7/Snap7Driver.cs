using Sharp7;
using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Model;
using System.Buffers;
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

    public int ReadTag(Device device, Tag tag, Span<byte> data, int startOffset, int length)
    {
        Snap7Plc p = (Snap7Plc)device;

        byte[] tmp = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            int ret = p.ReadBytes(tag.TagId, startOffset, tmp, length);
            if (ret != 0)
                return ret;

            tmp.AsSpan(0, length).CopyTo(data.Slice(startOffset - tag.ByteOffset, length));
            return 0;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(tmp);
        }
    }

    public int WriteTag(Device device, Tag tag, ReadOnlySpan<byte> data, int startOffset, int length)
    {
        Snap7Plc p = (Snap7Plc)device;

        byte[] tmp = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            data.Slice(startOffset - tag.ByteOffset, length).CopyTo(tmp);
            return p.WriteBytes(tag.TagId, startOffset, tmp, length);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(tmp);
        }
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
