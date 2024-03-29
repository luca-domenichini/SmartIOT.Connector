﻿using SmartIOT.Connector.Core.Conf;
using Swashbuckle.AspNetCore.Annotations;

namespace SmartIOT.Connector.RestApi.Model;

public class Scheduler
{
    /// <summary>
    /// Scheduler index
    /// </summary>
    [SwaggerSchema("Scheduler index", Nullable = false)]
    public int Index { get; }

    /// <summary>
    /// Scheduler name describing the devices
    /// </summary>
    [SwaggerSchema("Scheduler name describing the devices", Nullable = false)]
    public string Name { get; }

    /// <summary>
    /// Scheduler status
    /// </summary>
    [SwaggerSchema("Scheduler status", Nullable = false)]
    public bool Active { get; }

    /// <summary>
    /// The device attached to current scheduler
    /// </summary>
    [SwaggerSchema("The device attached to current scheduler", Nullable = false)]
    public DeviceConfiguration Device { get; }

    public Scheduler(int index, string name, bool active, DeviceConfiguration device)
    {
        Index = index;
        Name = name;
        Active = active;
        Device = device;
    }
}
