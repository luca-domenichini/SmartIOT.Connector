namespace SmartIOT.Connector.Core.Events
{
    public class DeviceDriverRestartingEventArgs : EventArgs
    {
        public IDeviceDriver DeviceDriver { get; }

        public DeviceDriverRestartingEventArgs(IDeviceDriver deviceDriver)
        {
            DeviceDriver = deviceDriver;
        }
    }
}
