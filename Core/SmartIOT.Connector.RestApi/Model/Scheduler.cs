using SmartIOT.Connector.Core.Conf;

namespace SmartIOT.Connector.RestApi.Model
{
    public class Scheduler
    {
        /// <summary>
        /// Scheduler index
        /// </summary>
        public int Index { get; }

        /// <summary>
        /// Scheduler name describing the devices
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Scheduler status
        /// </summary>
        public bool Active { get; }

        /// <summary>
        /// The device attached to current scheduler
        /// </summary>
        public DeviceConfiguration Device { get; }

        public Scheduler(int index, string name, bool active, DeviceConfiguration device)
        {
            Index = index;
            Name = name;
            Active = active;
            Device = device;
        }
    }
}
