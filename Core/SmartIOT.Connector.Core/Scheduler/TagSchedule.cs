using SmartIOT.Connector.Core.Model;

namespace SmartIOT.Connector.Core.Scheduler
{
    public class TagSchedule
    {
        public Device Device { get; }
        public Tag Tag { get; }
        public TagScheduleType Type { get; }

        public TagSchedule(Device device, Tag tag, TagScheduleType type)
        {
            Device = device;
            Tag = tag;
            Type = type;
        }
    }
}
