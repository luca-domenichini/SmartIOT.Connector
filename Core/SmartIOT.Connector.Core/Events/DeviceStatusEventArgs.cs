namespace SmartIOT.Connector.Core.Events
{
    public class DeviceStatusEventArgs : EventArgs
    {
        public IDeviceDriver DeviceDriver { get; }
        public DeviceStatusEvent DeviceStatusEvent { get; }

        public DeviceStatusEventArgs(IDeviceDriver deviceDriver, DeviceStatusEvent deviceStatusEvent)
        {
            DeviceDriver = deviceDriver;
            DeviceStatusEvent = deviceStatusEvent;
        }
    }
}
