﻿using SnapModbus;

namespace SmartIOT.Connector.Plc.SnapModBus;

public class SnapModBusNode : Core.Model.Device
{
    public SnapMBBroker Client { get; init; }
    public new SnapModBusNodeConfiguration Configuration => (SnapModBusNodeConfiguration)base.Configuration;
    public bool IsConnected { get; private set; }

    public SnapModBusNode(SnapModBusNodeConfiguration deviceConfiguration) : base(deviceConfiguration)
    {
        Client = new SnapMBBroker(MBConsts.ProtoTCP, deviceConfiguration.IpAddress, deviceConfiguration.Port);
    }

    public int Connect()
    {
        int err = Client.Connect();

        IsConnected = err == 0;

        return err;
    }

    public int Disconnect()
    {
        try
        {
            return Client.Disconnect();
        }
        finally
        {
            IsConnected = false;
        }
    }

    public int ReadRegisters(ushort address, ushort amount, ushort[] data)
    {
        return Client.ReadInputRegisters(Configuration.NodeId, address, amount, data);
    }

    public int WriteRegisters(ushort address, ushort[] data, ushort amount)
    {
        return Client.WriteMultipleRegisters(Configuration.NodeId, address, amount, data);
    }
}
