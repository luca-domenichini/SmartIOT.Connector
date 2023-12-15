using SmartIOT.Connector.Core.Conf;
using SmartIOT.Connector.Core.Factory;
using SmartIOT.Connector.Mocks;
using System.Collections.Generic;

namespace SmartIOT.Connector.Core.Tests;

public class SmartIOTBaseTests
{
    protected virtual SmartIotConnector SetupSmartIotConnector(SmartIotConnectorConfiguration configuration, ISchedulerFactory schedulerFactory, ITimeService timeService, IConnector connector)
    {
        return new SmartIotConnectorBuilder()
            .WithAutoDiscoverDeviceDriverFactories()
            .WithAutoDiscoverConnectorFactories()
            .WithConfiguration(configuration)
            .WithSchedulerFactory(schedulerFactory)
            .WithTimeService(timeService)
            .AddConnector(connector)
            .Build();
    }

    protected virtual SmartIotConnector SetupSmartIotConnector(SmartIotConnectorConfiguration configuration, ISchedulerFactory schedulerFactory, IConnector connector)
    {
        return SetupSmartIotConnector(configuration, schedulerFactory, new TimeService(), connector);
    }

    protected virtual SmartIotConnector SetupSmartIotConnector(SmartIotConnectorConfiguration configuration, IConnector connector)
    {
        return SetupSmartIotConnector(configuration, new SchedulerFactory(), new TimeService(), connector);
    }

    protected virtual SmartIotConnector SetupSmartIotConnector(SmartIotConnectorConfiguration configuration)
    {
        return SetupSmartIotConnector(configuration, new SchedulerFactory(), new TimeService(), new FakeConnector());
    }

    protected virtual SmartIotConnector SetupSmartIotConnector()
    {
        return SetupSmartIotConnector(SetupSampleConfiguration(), new SchedulerFactory(), new TimeService(), new FakeConnector());
    }

    protected virtual SmartIotConnector SetupSmartIotConnector(IConnector connector)
    {
        return SetupSmartIotConnector(SetupSampleConfiguration(), new SchedulerFactory(), new TimeService(), connector);
    }

    protected virtual SmartIotConnectorConfiguration SetupSampleConfiguration()
    {
        return SetupConfiguration(new List<DeviceConfiguration>()
            {
                new DeviceConfiguration("snap7://Ip=192.168.0.11;SlotNo=0;RackNo=0;Type=BASIC", "1", true, "Snap7Plc", new List<TagConfiguration>()
                {
                    new TagConfiguration("DB20", TagType.READ, 10, 100, 1),
                    new TagConfiguration("DB22", TagType.WRITE, 10, 100, 1),
                }),
                new DeviceConfiguration("s7net://Ip=192.168.0.12;SlotNo=0;RackNo=0;Type=BASIC", "2", true, "S7Net7Plc", new List<TagConfiguration>()
                {
                    new TagConfiguration("DB20", TagType.READ, 10, 100, 1),
                    new TagConfiguration("DB22", TagType.WRITE, 10, 100, 1),
                }),
            }
        );
    }

    protected virtual SmartIotConnectorConfiguration SetupConfiguration(List<DeviceConfiguration> deviceConfigurations)
    {
        return new SmartIotConnectorConfiguration()
        {
            SchedulerConfiguration = new SchedulerConfiguration()
            {
                TerminateAfterNoWriteRequestsDelayMillis = 0,
                TerminateMinimumDelayMillis = 0
            },
            DeviceConfigurations = deviceConfigurations
        };
    }

    protected virtual SmartIotConnectorConfiguration SetupConfiguration(DeviceConfiguration deviceConfiguration)
    {
        return new SmartIotConnectorConfiguration()
        {
            SchedulerConfiguration = new SchedulerConfiguration()
            {
                TerminateAfterNoWriteRequestsDelayMillis = 0,
                TerminateMinimumDelayMillis = 0
            },
            DeviceConfigurations = new List<DeviceConfiguration>() { deviceConfiguration }
        };
    }
}
