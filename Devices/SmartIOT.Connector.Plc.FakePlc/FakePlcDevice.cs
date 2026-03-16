using SmartIOT.Connector.Core.Model;

namespace SmartIOT.Connector.Plc.FakePlc;

/// <summary>
/// An in-memory fake PLC device.
/// <para>
/// Each tag has an independent backing buffer (<see cref="GetTagBuffer"/>) that tests
/// can read and write freely to simulate device-side data changes.  All buffers are
/// indexed by absolute byte offset, matching <see cref="Tag.ByteOffset"/> semantics,
/// so callers must account for the tag's start offset when addressing individual bytes.
/// </para>
/// </summary>
public class FakePlcDevice : Device
{
    /// <summary>Offset → backing buffer for each tag, keyed by <see cref="Tag.TagId"/>.</summary>
    private readonly Dictionary<string, byte[]> _tagBuffers = new();

    public bool IsConnected { get; private set; }

    public FakePlcDevice(FakePlcConfiguration configuration) : base(configuration)
    {
        foreach (var tag in Tags)
            _tagBuffers[tag.TagId] = new byte[tag.Size];
    }

    /// <summary>
    /// Returns the raw backing buffer for the given tag.  The buffer has length
    /// <c>tag.Size</c> and index 0 corresponds to <c>tag.ByteOffset</c>.
    /// Tests can modify the returned array directly to inject data.
    /// </summary>
    public byte[] GetTagBuffer(string tagId)
    {
        if (!_tagBuffers.TryGetValue(tagId, out var buffer))
            throw new ArgumentException($"Tag '{tagId}' not found in FakePlcDevice '{Name}'.", nameof(tagId));
        return buffer;
    }

    /// <summary>Convenience overload keyed on the <see cref="Tag"/> instance.</summary>
    public byte[] GetTagBuffer(Tag tag) => GetTagBuffer(tag.TagId);

    internal void SetConnected(bool connected) => IsConnected = connected;
}
