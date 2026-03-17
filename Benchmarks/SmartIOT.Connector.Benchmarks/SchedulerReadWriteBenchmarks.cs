using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Model;
using SmartIOT.Connector.Core.Scheduler;
using SmartIOT.Connector.Plc.FakePlc;

namespace SmartIOT.Connector.Benchmarks;

/// <summary>
/// Benchmarks for the read/write scheduling hot path in <see cref="TagSchedulerEngine"/>.
///
/// Each benchmark parameter N represents the number of consecutive ScheduleNextTag cycles
/// (read or write) executed in a single benchmark iteration.  This simulates the real
/// scheduler loop and exercises:
///   - <see cref="TagSchedulerEngine.ScheduleNextTag"/>
///   - <see cref="FakeDriver.ReadTag"/> / <see cref="FakeDriver.WriteTag"/>
///   - <see cref="TagSchedulerEngine"/> change detection, bounds calculation, event raising
///
/// Run with:   dotnet run -c Release --project Benchmarks/SmartIOT.Connector.Benchmarks
/// </summary>
[Config(typeof(BenchmarkConfig))]
[MemoryDiagnoser]
public class SchedulerReadWriteBenchmarks
{
    // -----------------------------------------------------------------------
    // Parameters
    // -----------------------------------------------------------------------

    [Params(1, 10, 100, 1000)]
    public int N { get; set; }

    // -----------------------------------------------------------------------
    // Infrastructure shared between benchmarks
    // -----------------------------------------------------------------------

    private TagSchedulerEngine _engine = null!;
    private FakePlcDevice _plcDevice = null!;
    private Tag _readTag = null!;
    private Tag _writeTag = null!;
    private TagSchedule _readSchedule = null!;
    private TagSchedule _writeSchedule = null!;

    // -----------------------------------------------------------------------
    // Setup / Teardown
    // -----------------------------------------------------------------------

    [GlobalSetup]
    public void Setup()
    {
        // Build a device with one 200-byte READ tag and one 200-byte WRITE tag.
        var deviceConfig = new DeviceConfiguration
        {
            ConnectionString = "fakeplc://bench-device",
            DeviceId = "bench-device",
            Name = "BenchDevice",
            Enabled = true,
            Tags =
            [
                new TagConfiguration("1", TagType.READ,  byteOffset: 0,   size: 200, weight: 1),
                new TagConfiguration("2", TagType.WRITE, byteOffset: 200, size: 200, weight: 1),
            ]
        };

        var fakeCfg = new FakePlcConfiguration(deviceConfig);
        _plcDevice = new FakePlcDevice(fakeCfg);

        var driver = new FakeDriver(_plcDevice);

        // Zero wait times so every tag is always immediately schedulable.
        var schedulerConfig = new SchedulerConfiguration
        {
            WaitTimeBetweenEveryScheduleMillis = 0,
            WaitTimeBetweenReadSchedulesMillis = 0,
            WaitTimeAfterErrorMillis = 0,
        };

        _engine = new TagSchedulerEngine(driver, new TimeService(), schedulerConfig);

        // Wire up event handlers so the engine doesn't throw on unhandled events.
        _engine.TagReadEvent += (_, _) => { };
        _engine.TagWriteEvent += (_, _) => { };
        _engine.DeviceStatusEvent += (_, _) => { };
        _engine.ExceptionHandler += (_, _) => { };

        // Perform a full driver restart so the device is connected and all tags are initialised.
        _engine.RestartDriver();

        // Cache tag references and pre-built schedules for minimal overhead in the hot loop.
        _readTag  = _plcDevice.Tags.First(t => t.TagType == TagType.READ);
        _writeTag = _plcDevice.Tags.First(t => t.TagType == TagType.WRITE);

        _readSchedule  = new TagSchedule(_plcDevice, _readTag,  TagScheduleType.READ);
        _writeSchedule = new TagSchedule(_plcDevice, _writeTag, TagScheduleType.WRITE);

        // Pre-populate write tag's backing buffer and request synchronisation so
        // the first write benchmark iteration finds something to do.
        SeedWriteData();
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>Fills the write-tag backing buffer with non-zero data and requests a sync.</summary>
    private void SeedWriteData()
    {
        var buf = _plcDevice.GetTagBuffer(_writeTag);
        for (int i = 0; i < buf.Length; i++)
            buf[i] = (byte)(i + 1);

        // Simulate an external write request so the scheduler treats the tag as dirty.
        _writeTag.TryMergeData(buf, _writeTag.ByteOffset);
    }

    /// <summary>Mutates every byte of the read-tag buffer so change detection always finds work.</summary>
    private void FlipReadData()
    {
        var buf = _plcDevice.GetTagBuffer(_readTag);
        for (int i = 0; i < buf.Length; i++)
            buf[i] ^= 0xFF;
    }

    // -----------------------------------------------------------------------
    // Benchmarks
    // -----------------------------------------------------------------------

    /// <summary>
    /// Pure read path: N consecutive full-tag read cycles through the engine.
    /// Each cycle: read driver → change detection → event raising.
    /// </summary>
    [Benchmark(Description = "ReadCycles")]
    public void ReadCycles()
    {
        for (int i = 0; i < N; i++)
        {
            // Mutate the backing buffer so ParseTagChangesAndMerge always finds changes
            // and produces a real TagScheduleEvent with data (not an empty one).
            //FlipReadData();
            _engine.ScheduleTag(_readSchedule);
        }
    }

    /// <summary>
    /// Pure write path: N consecutive full-tag write cycles through the engine.
    /// Each cycle: bounds detection → WriteTag driver call → OldData sync → event raising.
    /// </summary>
    [Benchmark(Description = "WriteCycles")]
    public int WriteCycles()
    {
        int i;
        for (i = 0; i < N; i++)
        {
            // Request a new write every iteration so the scheduler doesn't skip it.
            //SeedWriteData();
            _engine.ScheduleTag(_writeSchedule);
        }

        return i;
    }

    /// <summary>
    /// Interleaved read+write path: alternates one read cycle and one write cycle, N times each.
    /// Total operations per iteration = 2·N.
    /// </summary>
    [Benchmark(Description = "ReadWriteInterleaved")]
    public int ReadWriteInterleaved()
    {
        int i;
        for (i = 0; i < N; i++)
        {
            // FlipReadData();
            _engine.ScheduleTag(_readSchedule);

            // SeedWriteData();
            _engine.ScheduleTag(_writeSchedule);
        }

        return i;
    }

    /// <summary>
    /// Full scheduler selection path: uses <see cref="TagSchedulerEngine.ScheduleNextTag"/>
    /// for N iterations, letting the engine pick which tag to schedule.
    /// Stresses <see cref="TagSchedulerEngine.GetNextTagSchedule"/> as well as the driver.
    /// </summary>
    [Benchmark(Description = "ScheduleNextTag")]
    public int ScheduleNextTagCycles()
    {
        int i;
        for (i = 0; i < N; i++)
        {
            // Keep alternating data mutations so there is always something meaningful to do.
            // if (i % 2 == 0)
                //FlipReadData();
            // else
                //SeedWriteData();

            _engine.ScheduleNextTag(scheduleWritesOnly: false);
        }
        return i;
    }
}

// ---------------------------------------------------------------------------
// Config
// ---------------------------------------------------------------------------

internal class BenchmarkConfig : ManualConfig
{
    public BenchmarkConfig()
    {
        AddJob(Job.Default
            .WithId("net10")
            .WithWarmupCount(3)
            .WithIterationCount(10));
    }
}
