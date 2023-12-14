using SmartIOT.Connector.Core.Conf;

namespace SmartIOT.Connector.Core.Factory;

public class DeviceDriverFactory : IDeviceDriverFactory
{
    private readonly List<IDeviceDriverFactory> _factories = new List<IDeviceDriverFactory>();

    public void Add(IDeviceDriverFactory factory)
    {
        _factories.Add(factory);
    }

    public void AddRange(IList<IDeviceDriverFactory> deviceDriverFactories)
    {
        _factories.AddRange(deviceDriverFactories);
    }

    public bool Any() => _factories.Count > 0;

    public bool Any(Func<object, bool> predicate) => _factories.Exists(x => predicate.Invoke(x));

    public IDeviceDriver? CreateDriver(DeviceConfiguration deviceConfiguration)
    {
        foreach (var factory in _factories)
        {
            var d = factory.CreateDriver(deviceConfiguration);
            if (d != null)
                return d;
        }

        return null;
    }
}
