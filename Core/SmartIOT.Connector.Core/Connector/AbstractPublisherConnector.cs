using SmartIOT.Connector.Core.Events;

namespace SmartIOT.Connector.Core.Connector
{
    public abstract class AbstractPublisherConnector : AbstractBufferedAggregatingConnector
    {
        public ConnectorOptions Options { get; }

        protected AbstractPublisherConnector(ConnectorOptions options)
            : base(options.ConnectionString)
        {
            Options = options;
        }

        protected abstract Task PublishTagScheduleEventAsync(TagScheduleEvent e);

        protected abstract Task PublishDeviceStatusEventAsync(DeviceStatusEvent e);

        protected abstract Task PublishExceptionAsync(Exception exception);

        protected override async Task HandleOnExceptionAsync(object? sender, ExceptionEventArgs args)
        {
            await PublishExceptionAsync(args.Exception);
        }

        protected override async Task HandleOnDeviceStatusEventAsync(object? sender, DeviceStatusEventArgs args)
        {
            await PublishDeviceStatusEventAsync(args.DeviceStatusEvent);
        }

        protected override async Task HandleOnTagReadEventAsync(object? sender, TagScheduleEventArgs args)
        {
            await PublishTagScheduleEventAsync(args.TagScheduleEvent);
        }

        protected override async Task HandleOnTagWriteEventAsync(object? sender, TagScheduleEventArgs args)
        {
            if (Options.IsPublishWriteEvents)
                await PublishTagScheduleEventAsync(args.TagScheduleEvent);
        }
    }
}
