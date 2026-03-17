using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Model;
using System.Buffers;
using System.Runtime.InteropServices;
using SnapModbus;

namespace SmartIOT.Connector.Plc.SnapModBus;

public class SnapModBusDriver : IDeviceDriver
{
    public string Name => $"{nameof(SnapModBusDriver)}.{Device.Name}";
    public Device Device { get; }

    public SnapModBusDriver(SnapModBusNode node)
    {
        Device = node;
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
            SnapModBusNode node = (SnapModBusNode)device;
            return node.Connect();
        }
    }

    public int Disconnect(Device device)
    {
        lock (device)
        {
            SnapModBusNode node = (SnapModBusNode)device;
            return node.Disconnect();
        }
    }

    public int ReadTag(Device device, Tag tag, Span<byte> data, int startOffset, int length)
    {
        SnapModBusNode node = (SnapModBusNode)device;

        // ModBus works with ushorts! We need to adapt length and startOffset accordingly
        // bytes -> ushorts | byte slots -> ushort slots
        // 0,1 -> 0,1 | [.     ] -> [.  ]
        // 0,2 -> 0,1 | [..    ] -> [.  ]
        // 0,3 -> 0,2 | [...   ] -> [.. ]
        // 0,4 -> 0,2 | [....  ] -> [.. ]
        // 0,5 -> 0,3 | [..... ] -> [...]
        // 0,6 -> 0,3 | [......] -> [...]
        // 1,1 -> 0,1 | [ .    ] -> [.  ]
        // 1,2 -> 0,2 | [ ..   ] -> [.. ]
        // 1,3 -> 0,2 | [ ...  ] -> [.. ]
        // 1,4 -> 0,3 | [ .... ] -> [...]
        // 1,5 -> 0,3 | [ .....] -> [...]
        // 2,1 -> 1,1 | [  .   ] -> [ . ]
        // 2,2 -> 1,1 | [  ..  ] -> [ . ]
        // 2,3 -> 1,2 | [  ... ] -> [ ..]
        // 2,4 -> 1,2 | [  ....] -> [ ..]
        ushort address = (ushort)(startOffset / 2);
        int endAddress = (startOffset + length - 1) / 2;
        ushort amount = (ushort)(endAddress - address + 1);

        ushort[] tmp = ArrayPool<ushort>.Shared.Rent(amount);
        try
        {
            // address is 1-based
            int ret = node.ReadRegisters((ushort)(address + 1), amount, tmp);
            if (ret != 0)
                return ret;

            if (node.Configuration.SwapBytes)
                CopyArrayAndSwapBytes(tmp, startOffset % 2, data, startOffset - tag.ByteOffset, length);
            else
                MemoryMarshal.Cast<ushort, byte>(tmp.AsSpan(0, amount)).Slice(startOffset % 2, length).CopyTo(data.Slice(startOffset - tag.ByteOffset, length));

            return 0;
        }
        finally
        {
            ArrayPool<ushort>.Shared.Return(tmp);
        }
    }

    public int WriteTag(Device device, Tag tag, ReadOnlySpan<byte> data, int startOffset, int length)
    {
        SnapModBusNode node = (SnapModBusNode)device;

        // ModBus works with ushorts! We need to adapt length and startOffset accordingly
        // bytes -> ushorts | byte slots -> ushort slots
        // 0,1 -> 0,1 | [.     ] -> [.  ]
        // 0,2 -> 0,1 | [..    ] -> [.  ]
        // 0,3 -> 0,2 | [...   ] -> [.. ]
        // 0,4 -> 0,2 | [....  ] -> [.. ]
        // 0,5 -> 0,3 | [..... ] -> [...]
        // 0,6 -> 0,3 | [......] -> [...]
        // 1,1 -> 0,1 | [ .    ] -> [.  ]
        // 1,2 -> 0,2 | [ ..   ] -> [.. ]
        // 1,3 -> 0,2 | [ ...  ] -> [.. ]
        // 1,4 -> 0,3 | [ .... ] -> [...]
        // 1,5 -> 0,3 | [ .....] -> [...]
        // 2,1 -> 1,1 | [  .   ] -> [ . ]
        // 2,2 -> 1,1 | [  ..  ] -> [ . ]
        // 2,3 -> 1,2 | [  ... ] -> [ ..]
        // 2,4 -> 1,2 | [  ....] -> [ ..]
        ushort address = (ushort)(startOffset / 2);
        int endAddress = (startOffset + length - 1) / 2;
        ushort amount = (ushort)(endAddress - address + 1);

        int startRemainder = startOffset % 2;
        int endRemainder = (startOffset + length) % 2;
        int copyLength = length + startRemainder + endRemainder;

        ushort[] tmp = ArrayPool<ushort>.Shared.Rent(amount);
        try
        {
            if (node.Configuration.SwapBytes)
                CopyArrayAndSwapBytes(data, startOffset - tag.ByteOffset - startRemainder, tmp, 0, copyLength);
            else
                data.Slice(startOffset - tag.ByteOffset - startRemainder, copyLength).CopyTo(MemoryMarshal.Cast<ushort, byte>(tmp.AsSpan(0, amount)));

            // address is 1-based
            return node.WriteRegisters((ushort)(address + 1), tmp, amount);
        }
        finally
        {
            ArrayPool<ushort>.Shared.Return(tmp);
        }
    }

    private static void CopyArrayAndSwapBytes(ReadOnlySpan<byte> source, int srcIndex, ushort[] target, int targetIndex, int length)
    {
        for (int i = 0; i < length; i += 2)
        {
            target[targetIndex + i / 2] = (ushort)((source[srcIndex + i] << 8) + source[srcIndex + i + 1]);
        }
    }

    private static void CopyArrayAndSwapBytes(ushort[] source, int srcIndex, Span<byte> target, int targetIndex, int length)
    {
        for (int i = 0; i < length; i += 2)
        {
            var word = source[srcIndex + i / 2];

            target[targetIndex + i] = (byte)(word >> 8);
            target[targetIndex + i + 1] = (byte)(word);
        }
    }

    public string GetErrorMessage(int errorNumber)
    {
        return MB.ErrorText(errorNumber);
    }

    public string GetDeviceDescription(Device device) => device.Name;
}
