using Moq;
using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Events;
using SmartIOT.Connector.Messages;

namespace SmartIOT.Connector.Mocks
{
    public class MockConnector : Mock<IConnector>, IConnector
    {
        private ISmartIOTConnectorInterface? _connectorInterface;
        public string ConnectionString => "mock://";

        public async Task StartAsync(ISmartIOTConnectorInterface connectorInterface)
        {
            _connectorInterface = connectorInterface;
            await Object.StartAsync(connectorInterface);
        }

        public async Task StopAsync()
        {
            await Object.StopAsync();
        }

        public void RequestTagWrite(TagWriteRequestCommand command)
        {
            _connectorInterface!.RequestTagWrite(command.DeviceId, command.TagId, command.StartOffset, command.Data);
        }

        public void OnTagReadEvent(object? sender, TagScheduleEventArgs args)
        {
            Object.OnTagReadEvent(sender, args);
        }

        public void OnTagWriteEvent(object? sender, TagScheduleEventArgs args)
        {
            Object.OnTagWriteEvent(sender, args);
        }

        public void OnDeviceStatusEvent(object? sender, DeviceStatusEventArgs args)
        {
            Object.OnDeviceStatusEvent(sender, args);
        }

        public void OnException(object? sender, ExceptionEventArgs args)
        {
            Object.OnException(sender, args);
        }
    }
}
