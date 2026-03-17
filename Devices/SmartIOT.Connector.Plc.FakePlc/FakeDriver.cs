using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Model;
using System.Text;

namespace SmartIOT.Connector.Plc.FakePlc;

/// <summary>
/// In-memory fake device driver backed by <see cref="FakePlcDevice"/>.
/// <para>
/// <b>Read:</b> copies bytes from the tag's backing buffer (<see cref="FakePlcDevice.GetTagBuffer"/>)
/// into the <paramref name="data"/> array slice requested by the scheduler.<br/>
/// <b>Write:</b> copies the bytes from the scheduler's <paramref name="data"/> slice into the
/// tag's backing buffer so that tests can observe what was "written to the device".
/// </para>
/// No I/O, no delays — purely synchronous in-memory copies.
/// </summary>
public class FakeDriver : IDeviceDriver
{
    public string Name => $"{nameof(FakeDriver)}.{Device.Name}";
    public Device Device { get; }

    private FakePlcDevice PlcDevice => (FakePlcDevice)Device;

    public FakeDriver(FakePlcDevice device)
    {
        Device = device;
    }

    public int StartInterface() => 0;

    public int StopInterface() => 0;

    public int Connect(Device device)
    {
        PlcDevice.SetConnected(true);
        return 0;
    }

    public int Disconnect(Device device)
    {
        PlcDevice.SetConnected(false);
        return 0;
    }

    /// <summary>
    /// Copies <paramref name="length"/> bytes starting at absolute offset
    /// <paramref name="startOffset"/> from the tag's backing buffer into <paramref name="data"/>.
    /// </summary>
    public int ReadTag(Device device, Tag tag, Span<byte> data, int startOffset, int length)
    {
        var buffer = PlcDevice.GetTagBuffer(tag.TagId);
        // buffer index: startOffset - tag.ByteOffset
        int srcIndex = startOffset - tag.ByteOffset;
        buffer.AsSpan(srcIndex, length).CopyTo(data.Slice(srcIndex, length));
        return 0;
    }

    /// <summary>
    /// Copies <paramref name="length"/> bytes from <paramref name="data"/> (at absolute offset
    /// <paramref name="startOffset"/>) into the tag's backing buffer so tests can observe writes.
    /// </summary>
    public int WriteTag(Device device, Tag tag, ReadOnlySpan<byte> data, int startOffset, int length)
    {
        var buffer = PlcDevice.GetTagBuffer(tag.TagId);
        int dstIndex = startOffset - tag.ByteOffset;
        data.Slice(dstIndex, length).CopyTo(buffer.AsSpan(dstIndex, length));
        return 0;
    }

    public string GetErrorMessage(int errorNumber) => $"FakePlc error {errorNumber}";

    public string GetDeviceDescription(Device device)
    {
        var sb = new StringBuilder();
        sb.Append("FakePlc '").Append(device.Name).Append("'");
        if (PlcDevice.IsConnected)
            sb.Append(" [connected]");
        else
            sb.Append(" [disconnected]");
        return sb.ToString();
    }
}
