using SmartIOT.Connector.Core;
using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Factory;
using SmartIOT.Connector.RestApi.Model;

namespace SmartIOT.Connector.RestApi.Services
{
    public class DeviceService : IDeviceService
    {
        private readonly SmartIotConnector _smartIotConnector;
        private readonly ISchedulerFactory _schedulerFactory;
        private readonly IDeviceDriverFactory _deviceDriverFactory;
        private readonly ITimeService _timeService;

        public DeviceService(SmartIotConnector smartIotConnector, ISchedulerFactory schedulerFactory, IDeviceDriverFactory deviceDriverFactory, ITimeService timeService)
        {
            _smartIotConnector = smartIotConnector;
            _schedulerFactory = schedulerFactory;
            _deviceDriverFactory = deviceDriverFactory;
            _timeService = timeService;
        }

        public IList<DeviceConfiguration> GetDeviceConfigurations()
        {
            return _smartIotConnector.Schedulers.Select(x => x.Device)
                .Select(x => x.Configuration)
                .ToList();
        }

        public DeviceConfiguration? GetDeviceConfiguration(string deviceId)
        {
            return _smartIotConnector.Schedulers.Select(x => x.Device)
                .Where(x => string.Equals(x.DeviceId, deviceId, StringComparison.InvariantCultureIgnoreCase))
                .Select(x => x.Configuration)
                .FirstOrDefault();
        }

        public IList<Device> GetDevices()
        {
            return _smartIotConnector.Schedulers
                .Select(x => new Device(x.Device.DeviceId, x.Device.DeviceStatus, x.Device.Tags.Select(t => new Tag(t)).ToList()))
                .ToList();
        }

        public Device? GetDevice(string deviceId)
        {
            return _smartIotConnector.Schedulers
                .Where(x => x.Device.DeviceId.Equals(deviceId, StringComparison.InvariantCultureIgnoreCase))
                .Select(x => new Device(x.Device.DeviceId, x.Device.DeviceStatus, x.Device.Tags.Select(t => new Tag(t)).ToList()))
                .FirstOrDefault();
        }

        public void AddDevice(DeviceConfiguration deviceConfiguration)
        {
            var driver = _deviceDriverFactory.CreateDriver(deviceConfiguration);
            if (driver == null)
                throw new ApplicationException($"DeviceConfiguration not valid {deviceConfiguration.ConnectionString}");

            _smartIotConnector.AddScheduler(_schedulerFactory.CreateScheduler(driver.Name, driver, _timeService, _smartIotConnector.SchedulerConfiguration));
        }

        public void RemoveDevice(string deviceId)
        {
            var scheduler = _smartIotConnector.Schedulers
                .FirstOrDefault(x => x.Device.DeviceId.Equals(deviceId, StringComparison.InvariantCultureIgnoreCase));

            if (scheduler == null)
                throw new ApplicationException($"Device {deviceId} does not exists");

            _smartIotConnector.RemoveScheduler(scheduler);
        }

        public TagData? GetTagData(string deviceId, string tagId)
        {
            var device = _smartIotConnector.Schedulers
                .Select(x => x.Device)
                .FirstOrDefault(x => x.DeviceId.Equals(deviceId, StringComparison.InvariantCultureIgnoreCase));

            if (device == null)
                return null;

            var tag = device.Tags.FirstOrDefault(x => x.TagId.Equals(tagId, StringComparison.InvariantCultureIgnoreCase));
            if (tag == null)
                return null;

            return new TagData(tag.ByteOffset, tag.GetData());
        }

        public void SetTagData(string deviceId, string tagId, TagData tagData)
        {
            var device = _smartIotConnector.Schedulers
                .Select(x => x.Device)
                .FirstOrDefault(x => x.DeviceId.Equals(deviceId, StringComparison.InvariantCultureIgnoreCase));

            if (device == null)
                throw new ApplicationException($"Device {deviceId} does not exists");

            var tag = device.Tags.FirstOrDefault(x => x.TagId.Equals(tagId, StringComparison.InvariantCultureIgnoreCase));
            if (tag == null)
                throw new ApplicationException($"Tag {tagId} does not exists");

            if (tagData.StartOffset < tag.ByteOffset)
                throw new ApplicationException($"Requested StartOffset {tagData.StartOffset} < {tag.ByteOffset}");

            if (tagData.StartOffset + tagData.Bytes.Length > tag.ByteOffset + tag.Size)
                throw new ApplicationException($"Data packet is too big. Requested: [{tagData.StartOffset}..{tagData.StartOffset + tagData.Bytes.Length - 1}], accepted: [{tag.ByteOffset}..{tag.ByteOffset + tag.Size - 1}]");

            tag.RequestTagWrite(tagData.Bytes, tagData.StartOffset);
        }

        public Tag? GetTag(string deviceId, string tagId)
        {
            return _smartIotConnector.Schedulers
                .Where(x => x.Device.DeviceId.Equals(deviceId, StringComparison.InvariantCultureIgnoreCase))
                .SelectMany(x => x.Device.Tags)
                .Where(x => x.TagId.Equals(tagId, StringComparison.InvariantCultureIgnoreCase))
                .Select(x => new Tag(x))
                .FirstOrDefault();
        }

        public TagConfiguration? GetTagConfiguration(string deviceId, string tagId)
        {
            return _smartIotConnector.Schedulers
                .Where(x => x.Device.DeviceId.Equals(deviceId, StringComparison.InvariantCultureIgnoreCase))
                .SelectMany(x => x.Device.Tags)
                .Where(x => x.TagId.Equals(tagId, StringComparison.InvariantCultureIgnoreCase))
                .Select(x => x.TagConfiguration)
                .FirstOrDefault();
        }

        public void AddTag(string deviceId, TagConfiguration tagConfiguration)
        {
            var device = _smartIotConnector.Schedulers
                .Select(x => x.Device)
                .FirstOrDefault(x => x.DeviceId.Equals(deviceId, StringComparison.InvariantCultureIgnoreCase));

            if (device == null)
                throw new ApplicationException($"Device {deviceId} does not exists");

            if (device.Tags.Any(x => x.TagId.Equals(tagConfiguration.TagId, StringComparison.InvariantCultureIgnoreCase)))
                throw new ApplicationException($"Tag {tagConfiguration.TagId} already exists");

            device.AddTag(tagConfiguration);
        }

        public void RemoveTag(string deviceId, string tagId)
        {
            var device = _smartIotConnector.Schedulers
                .Select(x => x.Device)
                .FirstOrDefault(x => x.DeviceId.Equals(deviceId, StringComparison.InvariantCultureIgnoreCase));

            if (device == null)
                throw new ApplicationException($"Device {deviceId} does not exists");

            var tag = device.Tags.FirstOrDefault(x => x.TagId.Equals(tagId, StringComparison.InvariantCultureIgnoreCase));
            if (tag == null)
                throw new ApplicationException($"Tag {tagId} does not exists");

            device.RemoveTag(tag);
        }

        public void UpdateTag(string deviceId, TagConfiguration tagConfiguration)
        {
            var device = _smartIotConnector.Schedulers
                .Select(x => x.Device)
                .FirstOrDefault(x => x.DeviceId.Equals(deviceId, StringComparison.InvariantCultureIgnoreCase));

            if (device == null)
                throw new ApplicationException($"Device {deviceId} does not exists");

            var oldTag = device.Tags.FirstOrDefault(x => x.TagId.Equals(tagConfiguration.TagId, StringComparison.InvariantCultureIgnoreCase));
            if (oldTag == null)
                throw new ApplicationException($"Tag {tagConfiguration.TagId} does not exists");

            if (!device.UpdateTag(tagConfiguration))
                throw new ApplicationException($"Tag {tagConfiguration.TagId} does not exists");
        }
    }
}
