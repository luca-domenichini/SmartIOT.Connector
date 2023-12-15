using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Connector;
using SmartIOT.Connector.Core.Events;

namespace SmartIOT.Connector.Mocks;

public class FakeConnector : AbstractConnector
{
    private ISmartIOTConnectorInterface? _connectorInterface;

    public IList<TagScheduleEvent> TagReadEvents { get; } = new List<TagScheduleEvent>();
    public IList<TagScheduleEvent> TagWriteEvents { get; } = new List<TagScheduleEvent>();
    public IList<DeviceStatusEvent> DeviceStatusEvents { get; } = new List<DeviceStatusEvent>();
    public IList<ExceptionEventArgs> ExceptionEvents { get; } = new List<ExceptionEventArgs>();
    public IServiceProvider? ServiceProvider { get; }

    public FakeConnector() : base("fake://")
    {
    }

    // constructor for testing DI injection
    public FakeConnector(IServiceProvider serviceProvider) : base("fake://")
    {
        ServiceProvider = serviceProvider;
    }

    public override void OnTagReadEvent(object? sender, TagScheduleEventArgs args)
    {
        TagReadEvents.Add(args.TagScheduleEvent);
    }

    public override void OnTagWriteEvent(object? sender, TagScheduleEventArgs args)
    {
        TagWriteEvents.Add(args.TagScheduleEvent);
    }

    public override void OnDeviceStatusEvent(object? sender, DeviceStatusEventArgs args)
    {
        DeviceStatusEvents.Add(args.DeviceStatusEvent);
    }

    public override void OnException(object? sender, ExceptionEventArgs args)
    {
        ExceptionEvents.Add(args);
    }

    public void ClearEvents()
    {
        TagReadEvents.Clear();
        TagWriteEvents.Clear();
        DeviceStatusEvents.Clear();
        ExceptionEvents.Clear();
    }

    public override Task StartAsync(ISmartIOTConnectorInterface connectorInterface)
    {
        _connectorInterface = connectorInterface;
        return Task.CompletedTask;
    }

    public override Task StopAsync()
    {
        return Task.CompletedTask;
    }

    public void RequestTagWrite(string deviceId, string tagId, int startOffset, byte[] data)
    {
        _connectorInterface!.RequestTagWrite(deviceId, tagId, startOffset, data);
    }
}
