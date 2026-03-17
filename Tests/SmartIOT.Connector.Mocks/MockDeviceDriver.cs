using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Model;

namespace SmartIOT.Connector.Mocks;

/// <summary>
/// Callback delegate for ReadTag. The <paramref name="data"/> span is the full tag data buffer;
/// <paramref name="startOffset"/> and <paramref name="length"/> are the absolute offset and length
/// requested by the scheduler.
/// </summary>
public delegate void ReadTagCallbackDelegate(Tag tag, Span<byte> data, int startOffset, int length);

/// <summary>
/// A manually-implemented fake driver (no Moq) that supports <see cref="Span{T}"/> parameters.
/// Tracks ReadTag/WriteTag invocations so tests can verify call counts and arguments.
/// </summary>
public class MockDeviceDriver : IDeviceDriver
{
    public Core.Model.Device Device { get; }
    public string Name => $"MockDeviceDriver.{Device.Name}";

    public Action? StartInterfaceCallback { get; set; }
    public ReadTagCallbackDelegate? ReadTagCallback { get; set; }

    public int ConnectReturns { get; set; }
    public int DisconnectReturns { get; set; }
    public int ReadTagReturns { get; set; }
    public int WriteReturns { get; set; }
    public int StartInterfaceReturns { get; set; }
    public int StopInterfaceReturns { get; set; }

    private readonly List<(Core.Model.Device Device, Tag Tag, int StartOffset, int Length)> _readTagInvocations = new();
    private readonly List<(Core.Model.Device Device, Tag Tag, int StartOffset, int Length)> _writeTagInvocations = new();

    public IReadOnlyList<(Core.Model.Device Device, Tag Tag, int StartOffset, int Length)> ReadTagInvocations => _readTagInvocations;
    public IReadOnlyList<(Core.Model.Device Device, Tag Tag, int StartOffset, int Length)> WriteTagInvocations => _writeTagInvocations;

    public MockDeviceDriver(Core.Model.Device device, bool setupDefaults = true)
    {
        Device = device;
    }

    public int StartInterface()
    {
        StartInterfaceCallback?.Invoke();
        return StartInterfaceReturns;
    }

    public int StopInterface() => StopInterfaceReturns;

    public int Connect(Core.Model.Device device) => ConnectReturns;

    public int Disconnect(Core.Model.Device device) => DisconnectReturns;

    public string GetDeviceDescription(Core.Model.Device device) => device.Name;

    public string GetErrorMessage(int errorNumber) => $"{errorNumber}";

    public int ReadTag(Core.Model.Device device, Tag tag, Span<byte> data, int startOffset, int length)
    {
        _readTagInvocations.Add((device, tag, startOffset, length));
        ReadTagCallback?.Invoke(tag, data, startOffset, length);
        return ReadTagReturns;
    }

    public int WriteTag(Core.Model.Device device, Tag tag, ReadOnlySpan<byte> data, int startOffset, int length)
    {
        _writeTagInvocations.Add((device, tag, startOffset, length));
        return WriteReturns;
    }

    public void SetupReadTagAsRandomData()
    {
        var r = new Random();
        ReadTagCallback = (tag, data, startOffset, length) =>
        {
            Thread.Sleep(10); // some spare time to not stress the cpu
            for (int i = 0; i < data.Length; i++)
            {
                byte v;
                do
                {
                    v = (byte)r.Next(0, 255);
                } while (v == data[i]);

                data[i] = v;
            }
        };
    }

    public void SetupReadTagAsRandomData(int startOffset, int length)
    {
        var r = new Random();
        ReadTagCallback = (tag, data, s, l) =>
        {
            Thread.Sleep(10); // some spare time to not stress the cpu
            for (int i = 0; i < length; i++)
            {
                byte v;
                do
                {
                    v = (byte)r.Next(0, 255);
                } while (v == data[startOffset - tag.ByteOffset + i]);

                data[startOffset - tag.ByteOffset + i] = v;
            }
        };
    }

    /// <summary>Returns the number of times ReadTag was called with the given device, tag, startOffset and length.</summary>
    public int CountReadTag(Core.Model.Device device, Tag tag, int startOffset, int length)
        => _readTagInvocations.Count(x => x.Device == device && x.Tag == tag && x.StartOffset == startOffset && x.Length == length);

    /// <summary>Returns the number of times WriteTag was called with the given device, tag, startOffset and length.</summary>
    public int CountWriteTag(Core.Model.Device device, Tag tag, int startOffset, int length)
        => _writeTagInvocations.Count(x => x.Device == device && x.Tag == tag && x.StartOffset == startOffset && x.Length == length);

    public void ResetInvocations()
    {
        _readTagInvocations.Clear();
        _writeTagInvocations.Clear();
    }
}
