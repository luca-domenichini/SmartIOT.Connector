using SmartIOT.Connector.Core.Events;

namespace SmartIOT.Connector.Core.Connector;

public class AggregatingConnectorEventQueue : AggregatingQueue<CompositeConnectorEvent>
{
    protected override CompositeConnectorEvent? Aggregate(CompositeConnectorEvent item1, CompositeConnectorEvent item2)
    {
        if (item1.ExceptionEvent != null && item2.ExceptionEvent != null)
        {
            return AggregateExceptionEvents(item1.ExceptionEvent.Value.sender, item1.ExceptionEvent.Value.args, item2.ExceptionEvent.Value.args);
        }
        if (item1.DeviceStatusEvent != null && item2.DeviceStatusEvent != null)
        {
            return AggregateDeviceStatusEvents(item1.DeviceStatusEvent.Value.sender, item1.DeviceStatusEvent.Value.args, item2.DeviceStatusEvent.Value.args);
        }
        if (item1.TagReadScheduleEvent != null && item2.TagReadScheduleEvent != null)
        {
            return AggregateTagReadEvents(item1.TagReadScheduleEvent.Value.sender, item1.TagReadScheduleEvent.Value.args, item2.TagReadScheduleEvent.Value.args);
        }
        if (item1.TagWriteScheduleEvent != null && item2.TagWriteScheduleEvent != null)
        {
            return AggregateTagWriteEvents(item1.TagWriteScheduleEvent.Value.sender, item1.TagWriteScheduleEvent.Value.args, item2.TagWriteScheduleEvent.Value.args);
        }

        return null;
    }

    private CompositeConnectorEvent? AggregateTagReadEvents(object? sender, TagScheduleEventArgs item1, TagScheduleEventArgs item2)
    {
        var e1 = item1.TagScheduleEvent;
        var e2 = item2.TagScheduleEvent;

        if (e1.Tag == e2.Tag)
        {
            if (e1.Data != null && e2.Data != null)
            {
                int startOffset = Math.Min(e1.StartOffset, e2.StartOffset);
                int endOffset = Math.Max(e1.StartOffset + e1.Data.Length, e2.StartOffset + e2.Data.Length);
                int length = endOffset - startOffset;
                byte[] data = new byte[length];

                if (e1.StartOffset <= e2.StartOffset)
                {
                    Array.Copy(e1.Data, 0, data, 0, e1.Data.Length);
                    Array.Copy(e2.Data, 0, data, e2.StartOffset - e1.StartOffset, e2.Data.Length);
                }
                else
                {
                    Array.Copy(e1.Data, 0, data, e1.StartOffset - e2.StartOffset, e1.Data.Length);
                    Array.Copy(e2.Data, 0, data, 0, e2.Data.Length);
                }

                return CompositeConnectorEvent.TagRead((sender, new TagScheduleEventArgs(item2.DeviceDriver, TagScheduleEvent.BuildTagData(e1.Device, e1.Tag, startOffset, data, e1.IsErrorNumberChanged || e2.IsErrorNumberChanged))));
            }
            else if (e1.Data == null && e2.Data == null)
            {
                return CompositeConnectorEvent.TagRead((sender, item2), item1.TagScheduleEvent.IsErrorNumberChanged || item2.TagScheduleEvent.IsErrorNumberChanged);
            }
        }

        return null;
    }

    private CompositeConnectorEvent? AggregateTagWriteEvents(object? sender, TagScheduleEventArgs item1, TagScheduleEventArgs item2)
    {
        var e1 = item1.TagScheduleEvent;
        var e2 = item2.TagScheduleEvent;

        if (e1.Tag == e2.Tag && e1.Data != null && e2.Data != null)
        {
            int startOffset = Math.Min(e1.StartOffset, e2.StartOffset);
            int endOffset = Math.Max(e1.StartOffset + e1.Data.Length, e2.StartOffset + e2.Data.Length);
            int length = endOffset - startOffset;
            byte[] data = new byte[length];

            if (e1.StartOffset <= e2.StartOffset)
            {
                Array.Copy(e1.Data, 0, data, 0, e1.Data.Length);
                Array.Copy(e2.Data, 0, data, e2.StartOffset - e1.StartOffset, e2.Data.Length);
            }
            else
            {
                Array.Copy(e1.Data, 0, data, e1.StartOffset - e2.StartOffset, e1.Data.Length);
                Array.Copy(e2.Data, 0, data, 0, e2.Data.Length);
            }

            return CompositeConnectorEvent.TagWrite((sender, new TagScheduleEventArgs(item2.DeviceDriver, TagScheduleEvent.BuildTagData(e1.Device, e1.Tag, startOffset, data, e1.IsErrorNumberChanged || e2.IsErrorNumberChanged))));
        }

        return null;
    }

    private CompositeConnectorEvent? AggregateDeviceStatusEvents(object? sender, DeviceStatusEventArgs item1, DeviceStatusEventArgs item2)
    {
        if (item1.DeviceStatusEvent.Device == item2.DeviceStatusEvent.Device)
            return CompositeConnectorEvent.DeviceStatus((sender, item2));

        return null;
    }

    private CompositeConnectorEvent? AggregateExceptionEvents(object? sender, ExceptionEventArgs item1, ExceptionEventArgs item2)
    {
        return null;
    }
}
