# Byte Array Allocation & `Span<T>` Optimization Points

This document catalogues all identified opportunities to reduce heap allocations and adopt `Span<T>` / `Memory<T>` idioms in SmartIOT.Connector. Items are grouped by impact and difficulty.

---

## 1. `IDeviceDriver` interface — change `byte[]` to `Span<byte>`

**Files:** `Core/SmartIOT.Connector.Core/IDeviceDriver.cs`, all driver implementations

```csharp
// Current
int ReadTag(Device device, Tag tag, byte[] data, int startOffset, int length);
int WriteTag(Device device, Tag tag, byte[] data, int startOffset, int length);

// Proposed
int ReadTag(Device device, Tag tag, Span<byte> data, int startOffset, int length);
int WriteTag(Device device, Tag tag, ReadOnlySpan<byte> data, int startOffset, int length);
```

**Impact:** High. The engine calls these on every scheduler tick. Passing `Span<byte>` enables every driver to operate directly on slices of `tag.Data` without intermediate buffers. This is a prerequisite for several other optimizations below.

**Note:** `Span<T>` cannot be stored, so driver implementations that currently save the reference must be refactored to be purely synchronous (which they already are: all driver methods are synchronous and return `int`).

---

## 2. `Snap7Driver._tmp` intermediate buffer — eliminate both copies

**File:** `Devices/SmartIOT.Connector.Plc.Snap7/Snap7Driver.cs`

```csharp
// Current — ReadTag
int ret = p.ReadBytes(tag.TagId, startOffset, _tmp, length);
if (ret != 0) return ret;
Array.Copy(_tmp, 0, data, startOffset - tag.ByteOffset, length); // second copy

// Proposed — after IDeviceDriver → Span<byte> change
// Pass a slice directly:
int ret = p.ReadBytes(tag.TagId, startOffset, data.Slice(startOffset - tag.ByteOffset, length), length);
// No copy at all — data is written directly into tag.Data at the right offset

// Current — WriteTag
Array.Copy(data, startOffset - tag.ByteOffset, _tmp, 0, length);
return p.WriteBytes(tag.TagId, startOffset, _tmp, length);

// Proposed — after Span<byte> change in Sharp7 or with a small helper
// If Sharp7 ReadBytes/WriteBytes accept Span (or a buffer + offset):
return p.WriteBytes(tag.TagId, startOffset, data.Slice(startOffset - tag.ByteOffset, length), length);
```

**Impact:** High. Eliminates both the `_tmp` field (pre-allocated at max-tag-size bytes) and one `Array.Copy` per read and one `Array.Copy` per write operation. Every scheduler cycle fires at least one read or write.

**Constraint:** Sharp7's `S7Client.ReadBytes`/`WriteBytes` use `byte[]`. You may need a thin wrapper that pins a span with `fixed` or rents from `ArrayPool<byte>` for the call, then copies back — still a net win since the second `Array.Copy` is eliminated.

---

## 3. `TagChangeBounds` — class → struct

**File:** `Core/SmartIOT.Connector.Core/Scheduler/TagChangeBounds.cs`

```csharp
// Current
internal class TagChangeBounds { public int StartOffset; public int Length; }

// Proposed
internal struct TagChangeBounds { public int StartOffset; public int Length; }
```

**Impact:** Medium. `ParseWriteBounds` is called on every write schedule. It builds a `List<TagChangeBounds>`, allocating one heap object per change region per write. Converting to a struct keeps all entries inline in the `List<T>` backing array, eliminating per-entry heap allocations.

---

## 4. `ParseWriteBounds` — `List<TagChangeBounds>` allocation on every write

**File:** `Core/SmartIOT.Connector.Core/Scheduler/TagSchedulerEngine.cs`

```csharp
// Current
private static List<TagChangeBounds> ParseWriteBounds(Device device, Tag tag)
{
    var list = new List<TagChangeBounds>(); // allocated every write
    ...
}
```

**Proposed options (pick one):**

**Option A — Pass a pre-allocated list (reused per-scheduler-instance):**
```csharp
// Store on the engine:
private readonly List<TagChangeBounds> _writeBoundsBuffer = new();

// Reuse it:
_writeBoundsBuffer.Clear();
ParseWriteBounds(device, tag, _writeBoundsBuffer);
```

**Option B — `ValueListBuilder<T>` / `Span<TagChangeBounds>` (stackalloc for small counts):**
```csharp
// PDU count is bounded by tag.Size / SinglePDUWriteBytes, typically small
Span<TagChangeBounds> stackBuffer = stackalloc TagChangeBounds[32];
var list = new ValueListBuilder<TagChangeBounds>(stackBuffer);
```

**Impact:** Medium-High. Write operations are on the hot path (every scheduler write cycle). Combined with item 3 (struct), this eliminates all `List` and object allocations from `ParseWriteBounds`.

---

## 5. `ParseTagChangesAndMerge` — byte-by-byte `OldData` copy

**File:** `Core/SmartIOT.Connector.Core/Scheduler/TagSchedulerEngine.cs`

```csharp
// Current — copies modified bytes one at a time while scanning:
for (int i = 0; i < tag.Size; i++)
{
    if (tag.Data[i] != tag.OldData[i])
    {
        if (i < start) start = i;
        end = i;
        tag.OldData[i] = tag.Data[i]; // byte-by-byte copy
    }
}

// Proposed — scan only to find range, then bulk-copy the changed slice:
for (int i = 0; i < tag.Size; i++)
{
    if (tag.Data[i] != tag.OldData[i])
    {
        if (i < start) start = i;
        end = i;
    }
}
if (end >= 0)
{
    // Bulk copy with Span — more cache-friendly than byte-by-byte
    tag.Data.AsSpan(start, end - start + 1)
            .CopyTo(tag.OldData.AsSpan(start));
}
```

**Impact:** Low-Medium. JIT may already optimize the byte-by-byte loop, but the Span `CopyTo` call maps to `memmove` which is faster for contiguous ranges. The real benefit is cleaner code.

---

## 6. `WriteTag` — `Array.Copy` for `OldData` sync → `Span.CopyTo`

**File:** `Core/SmartIOT.Connector.Core/Scheduler/TagSchedulerEngine.cs`

```csharp
// Current
Array.Copy(tag.Data, 0, tag.OldData, 0, tag.Data.Length);

// Proposed
tag.Data.AsSpan().CopyTo(tag.OldData);
```

**Impact:** Minor stylistic/semantic improvement. `Span.CopyTo` is equivalent to `Buffer.BlockCopy` and expresses the intent more idiomatically. No allocation difference.

---

## 7. `TagScheduleEvent` — `BuildTagData` over-copies on initialization

**File:** `Core/SmartIOT.Connector.Core/Events/TagScheduleEvent.cs`

```csharp
// Called during restart/initialization to send full tag data:
public static TagScheduleEvent BuildTagData(Device device, Tag tag, bool isErrorNumberChanged)
{
    lock (tag)
    {
        byte[] data = new byte[tag.Data.Length]; // full tag copy
        Array.Copy(tag.Data, 0, data, 0, data.Length);
        return new TagScheduleEvent(device, tag, tag.ByteOffset, data, isErrorNumberChanged);
    }
}
```

**Proposed — add a `ReadOnlyMemory<byte>` overload for snapshot-free path:**

When the caller can guarantee the tag won't be modified before the event is consumed (e.g., during initialization under lock), the copy can be deferred by passing a `ReadOnlyMemory<byte>` slice of `tag.Data`, letting the final consumer (serializer) access the bytes directly:

```csharp
// New overload for initialization (caller holds tag lock):
internal static TagScheduleEvent BuildTagDataUnsafe(Device device, Tag tag, bool isErrorNumberChanged)
{
    // tag must be locked by the caller
    return new TagScheduleEvent(device, tag, tag.ByteOffset,
        new ReadOnlyMemory<byte>(tag.Data), isErrorNumberChanged);
}
```

Note this requires updating `TagScheduleEvent.Data` from `byte[]?` to `ReadOnlyMemory<byte>?`, which is a significant refactor. A transitional step is replacing `new byte[length] + Array.Copy` with `ArrayPool<byte>.Shared.Rent` and returning a pooled array with lifetime tracked by the event consumer (complex).

**Impact:** Medium. The full-tag copy on every restart initialization is less frequent, but reduces allocation pressure during reconnection storms.

---

## 8. `AggregatingConnectorEventQueue` — `byte[]` allocation on event merging

**File:** `Core/SmartIOT.Connector.Core/Connector/AggregatingConnectorEventQueue.cs`

```csharp
// Current — AggregateTagReadEvents / AggregateTagWriteEvents
byte[] data = new byte[length]; // new allocation per aggregation
Array.Copy(e1.Data, 0, data, 0, e1.Data.Length);
Array.Copy(e2.Data, 0, data, e2.StartOffset - e1.StartOffset, e2.Data.Length);
```

**Proposed — rent from `ArrayPool<byte>`:**
```csharp
byte[] data = ArrayPool<byte>.Shared.Rent(length);
try
{
    e1.Data.AsSpan().CopyTo(data);
    e2.Data.AsSpan().CopyTo(data.AsSpan(e2.StartOffset - e1.StartOffset));
    // Build event with exact-length slice:
    return CompositeConnectorEvent.TagRead((sender, new TagScheduleEventArgs(
        item2.DeviceDriver,
        TagScheduleEvent.BuildTagData(e1.Device, e1.Tag, startOffset, data.AsSpan(0, length), e1.IsErrorNumberChanged || e2.IsErrorNumberChanged))));
}
finally
{
    ArrayPool<byte>.Shared.Return(data);
}
```

**Impact:** Medium. Aggregation fires whenever two events for the same tag queue up back-to-back (very common at poll rates faster than consumer throughput). Pooling avoids repeated heap allocations.

**Constraint:** The final `TagScheduleEvent.BuildTagData` still allocates an exact-length `byte[]` to own the data for the event's lifetime. This can be eliminated only if `TagScheduleEvent.Data` is changed to `ReadOnlyMemory<byte>` backed by a pooled array with a controlled release.

---

## 9. `TryWithDeviceDriver` — lambda closure allocation on hot path

**File:** `Core/SmartIOT.Connector.Core/Scheduler/TagSchedulerEngine.cs`

```csharp
// Current — called on every read/write:
(err, description) = TryWithDeviceDriver(x => x.ReadTag(device, tag, tag.Data, tag.ByteOffset, tag.Size));
```

Every call allocates a heap `Func<IDeviceDriver, int>` closure capturing `device`, `tag`, `tag.Data`, etc. Because different local variables are captured each time, the compiler cannot cache a `static` delegate.

**Proposed — generic state-passing overload to avoid closure:**
```csharp
private (int, string) TryWithDeviceDriver<TState>(TState state, Func<IDeviceDriver, TState, int> action)
{
    int err;
    string description = string.Empty;
    try
    {
        err = action(DeviceDriver, state);
        if (err != 0) description = DeviceDriver.GetErrorMessage(err);
    }
    catch (Exception ex) { ... }
    return (err, description ?? "Unknown error");
}

// Call site — tuple state avoids closure:
(err, description) = TryWithDeviceDriver(
    (device, tag, tag.ByteOffset, tag.Size),
    static (driver, s) => driver.ReadTag(s.device, s.tag, s.tag.Data, s.ByteOffset, s.Size));
```

**Impact:** Low-Medium. Called on every scheduler tick (multiple times per device per interval). The `static` lambda with tuple state eliminates the closure heap allocation entirely.

---

## 10. `JsonStreamMessageSerializer.ReadLine` — per-byte read into `List<byte>`

**File:** `Core/SmartIOT.Connector.Messages/Serializers/JsonStreamMessageSerializer.cs`

```csharp
// Current — allocates List<byte> + ToArray() for every incoming message:
private string? ReadLine(Stream stream)
{
    var bytes = new List<byte>();
    int current;
    while ((current = stream.ReadByte()) != -1 && current != 0x0A)
        bytes.Add((byte)current);

    if (bytes.Count > 0)
        return Encoding.UTF8.GetString(bytes.ToArray()); // extra allocation

    return null;
}
```

**Proposed — `ArrayPool<byte>` buffer with direct decode:**
```csharp
private string? ReadLine(Stream stream)
{
    byte[] buffer = ArrayPool<byte>.Shared.Rent(4096);
    try
    {
        int count = 0;
        int current;
        while ((current = stream.ReadByte()) != -1 && current != 0x0A)
        {
            if (count == buffer.Length)
            {
                // grow: rent a larger buffer, copy, return old
                var larger = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
                buffer.AsSpan(0, count).CopyTo(larger);
                ArrayPool<byte>.Shared.Return(buffer);
                buffer = larger;
            }
            buffer[count++] = (byte)current;
        }

        if (count == 0) return null;

        return Encoding.UTF8.GetString(buffer.AsSpan(0, count));
        // Encoding.UTF8.GetString(ReadOnlySpan<byte>) — zero-copy decode, no ToArray()
    }
    finally
    {
        ArrayPool<byte>.Shared.Return(buffer);
    }
}
```

**Impact:** Medium. Eliminates two allocations per received message (`List<byte>` + `ToArray()`). Per-byte reading from network `NetworkStream` also benefits from buffered reading — consider wrapping in `BufferedStream` if not already.

---

## 11. `ProtobufSingleMessageSerializer` — `MemoryStream` per call

**File:** `Core/SmartIOT.Connector.Messages/Serializers/ProtobufSingleMessageSerializer.cs`

```csharp
// Current — serialization allocates MemoryStream + ToArray():
public byte[] SerializeMessage(object message)
{
    var stream = new MemoryStream();         // alloc 1
    Serializer.Serialize(stream, message);
    return stream.ToArray();                 // alloc 2
}

// Current — deserialization wraps byte[] in MemoryStream:
public T? DeserializeMessage<T>(byte[] bytes)
{
    return (T?)Serializer.Deserialize(typeof(T), new MemoryStream(bytes)); // alloc
}
```

**Proposed — protobuf-net v3 `Span<T>` API:**
```csharp
// Serialization with RecyclableMemoryStream (Microsoft.IO.RecyclableMemoryStream):
public byte[] SerializeMessage(object message)
{
    using var ms = _memoryStreamManager.GetStream();
    Serializer.Serialize(ms, message);
    return ms.ToArray(); // still one alloc, but MemoryStream is pooled
}

// Deserialization directly from ReadOnlyMemory<byte> (protobuf-net v3):
public T? DeserializeMessage<T>(ReadOnlyMemory<byte> bytes)
{
    return Serializer.Deserialize<T>(bytes);
}
```

**If the interface is upgraded to accept `ReadOnlyMemory<byte>`:**
```csharp
public interface ISingleMessageSerializer
{
    byte[] SerializeMessage(object message);      // keep for MQTT payload compat
    T? DeserializeMessage<T>(ReadOnlyMemory<byte> bytes); // avoid MemoryStream wrap
}
```

**Impact:** Medium. Called on every MQTT publish and receive. Removing the `MemoryStream` wrapper on deserialization is straightforward with protobuf-net v3's APIs.

---

## 12. `JsonSingleMessageSerializer` — deserialization from `byte[]` → `ReadOnlySpan<byte>`

**File:** `Core/SmartIOT.Connector.Messages/Serializers/JsonSingleMessageSerializer.cs`

```csharp
// Current
public T? DeserializeMessage<T>(byte[] bytes)
{
    return JsonSerializer.Deserialize<T>(bytes, _options);
}

// Proposed — accept ReadOnlySpan<byte> (no intermediate array required):
public T? DeserializeMessage<T>(ReadOnlySpan<byte> bytes)
{
    return JsonSerializer.Deserialize<T>(bytes, _options); // overload exists in System.Text.Json
}
```

**Impact:** Low-Medium. Allows callers (MQTT message received payloads) to pass `e.ApplicationMessage.PayloadSegment` directly as a span without converting to `byte[]` first. The payload array rental from MQTTnet can be returned sooner.

---

## 13. `MqttClientConnector.PublishTagScheduleEvent` — triple allocation per publish

**File:** `Connectors/SmartIOT.Connector.Mqtt/Client/MqttClientConnector.cs`

```csharp
// Current — three sequential allocations:
var evt = e.Data != null
    ? TagScheduleEvent.BuildTagData(e.Device, e.Tag, e.IsErrorNumberChanged) // alloc 1: full byte[] copy
    : e;
var message = evt.ToEventMessage(isInitializationData);                       // alloc 2: TagEvent object
var payload = _messageSerializer.SerializeMessage(message);                   // alloc 3: serialized byte[]

await _mqttClient.EnqueueAsync(new MqttApplicationMessageBuilder()
    .WithPayload(payload)
    ...);
```

**Proposed reduction — bypass intermediate `TagScheduleEvent` copy when serializing:**

If `TagEvent.Data` accepted `ReadOnlyMemory<byte>` and `ISingleMessageSerializer.SerializeMessage` wrote to a pooled buffer returning `IMemoryOwner<byte>`:

```csharp
// Serialize directly from tag.Data without BuildTagData intermediary:
using IMemoryOwner<byte> payload = _messageSerializer.SerializeTagEvent(
    e.Device.DeviceId, e.Tag.TagId, e.Tag.ByteOffset,
    new ReadOnlyMemory<byte>(e.Tag.Data), // direct — no copy
    isInitializationData);

await _mqttClient.EnqueueAsync(new MqttApplicationMessageBuilder()
    .WithPayload(payload.Memory)
    ...);
```

**Impact:** High potential, high refactor cost. Eliminating `BuildTagData`'s full-tag copy for every MQTT publish is the most impactful single change in the MQTT connector hot path.

---

## 14. `Tag.GetData()` — returns defensive copy

**File:** `Core/SmartIOT.Connector.Core/Model/Tag.cs`

```csharp
// Current
public byte[] GetData()
{
    var bytes = new byte[Data.Length];
    Array.Copy(Data, bytes, bytes.Length);
    return bytes;
}
```

**Proposed — add a `ReadOnlyMemory<byte>` accessor for read-only consumers:**
```csharp
/// <summary>Returns a thread-safe snapshot copy for external ownership.</summary>
public byte[] GetData()
{
    var bytes = new byte[Data.Length];
    Data.AsSpan().CopyTo(bytes);
    return bytes;
}

/// <summary>
/// Returns a read-only view of the tag data. Caller must hold the tag lock or accept
/// the data may change. Intended for short-lived reads under lock (e.g., REST API).
/// </summary>
public ReadOnlySpan<byte> GetDataSpan()
{
    lock (this) return Data.AsSpan(); // caution: span of pinned array — valid for synchronous use only
}
```

**Impact:** Low. `GetData()` is not on the hot path (scheduler uses `tag.Data` directly). The new overload benefits REST API or diagnostic consumers that don't need ownership.

---

## 15. `SnapModBus` / `S7Net` drivers — verify intermediate buffers

**Files:** `Devices/SmartIOT.Connector.Plc.SnapModBus/`, `Devices/SmartIOT.Connector.Plc.S7Net/`

Similar to `Snap7Driver`, verify that these drivers have intermediate `_tmp` byte arrays used to bridge the device library API to the `tag.Data` buffer. Apply the same span-slice + direct-write approach once `IDeviceDriver` signatures are updated (item 1).

---

## Summary Table

| # | Location | Allocation Eliminated | Effort |
|---|----------|-----------------------|--------|
| 1 | `IDeviceDriver` interface | Enabling change for 2, 15 | High |
| 2 | `Snap7Driver._tmp` | 1× copy per read, 1× copy per write | Medium (after #1) |
| 3 | `TagChangeBounds` class→struct | 1× heap obj per change boundary | Low |
| 4 | `ParseWriteBounds` list reuse | 1× `List<T>` per write cycle | Low |
| 5 | `ParseTagChangesAndMerge` bulk copy | Byte-by-byte → `Span.CopyTo` | Low |
| 6 | `WriteTag` OldData sync | Style/Span idiom | Trivial |
| 7 | `TagScheduleEvent.BuildTagData` | 1× `byte[]` per init event | High (requires `ReadOnlyMemory<byte>`) |
| 8 | `AggregatingConnectorEventQueue` | 1× `byte[]` per aggregation | Medium |
| 9 | `TryWithDeviceDriver` closure | 1× closure per scheduler tick | Medium |
| 10 | `JsonStreamMessageSerializer.ReadLine` | `List<byte>` + `ToArray()` per message | Medium |
| 11 | `ProtobufSingleMessageSerializer` | `MemoryStream` per serialize/deserialize | Medium |
| 12 | `JsonSingleMessageSerializer` | `byte[]` param → `ReadOnlySpan<byte>` | Low |
| 13 | `MqttClientConnector` publish pipeline | Full-tag `byte[]` + `TagEvent` per publish | High |
| 14 | `Tag.GetData()` | Add span accessor; existing copy is correct | Low |
| 15 | SnapModBus/S7Net drivers | Same as #2 | Medium (after #1) |

---

## Recommended Incremental Order

1. **Items 3, 4, 5, 6** — zero-risk, self-contained, no API changes.
2. **Items 10, 12** — self-contained serializer improvements, no interface changes.
3. **Item 9** — refactor `TryWithDeviceDriver` with generic state; isolated to engine.
4. **Item 11** — upgrade protobuf serializer; consider `RecyclableMemoryStream`.
5. **Item 8** — pool buffers in `AggregatingConnectorEventQueue`.
6. **Item 1 + 2 + 15** — update `IDeviceDriver` signatures; update all drivers together.
7. **Items 7 + 13** — largest refactor; move `TagScheduleEvent.Data` to `ReadOnlyMemory<byte>` and rework the MQTT publish pipeline.
