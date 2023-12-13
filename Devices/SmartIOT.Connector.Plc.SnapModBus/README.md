# SnapModBus Device configuration

This project provides a bridge to make SmartIOT.Connector able to use the [SnapModBus library](https://snapmodbus.sourceforge.io/) by Dave Nardella to connect to a device using ModBus protocol, and to exchange data defined in its registers.

The supported connection string is as follows (square brackets for optional parameters):

```text
snapmodbus://Ip=<plc ip address>;Port=<port (502)>;NodeId=<nodeId (1)>;swapBytes=<true or false (false)>
```

Paramters `Port`, `SwapBytes` and `NodeId` are optional.

The `TagId`s are currently ignored, since only the registers are read and written.
