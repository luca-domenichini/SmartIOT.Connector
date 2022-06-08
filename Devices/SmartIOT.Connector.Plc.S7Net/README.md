# S7Net Device configuration

This project provides a bridge to make SmartIOT.Connector able to use the [S7Net library](https://github.com/S7NetPlus/s7netplus) to connect to a Siemens PLC and to exchange data defined in its datablocks.

The supported connection string is as follows (square brackets for optional parameters):
<pre>s7net://Ip=&lt;plc ip address>[;Port=&lt;port>];CpuType=&lt;cpuType>;Rack=&lt;rack>;Slot=&lt;slot></pre>

The values for <code>Rack</code> and <code>Slot</code> should be taken from the hardware configuration in Simatic Manager.
Usually we have <code>Rack=0;Slot=2</code> for S7300 PLCs and <code>Rack=0;Slot=0</code> for S71500 PLCs.

The <code>CpuType</code> should be one of the following:
 - S7200: S7 200 cpu type
 - Logo0BA8: Siemens Logo 0BA8
 - S7200Smart: S7 200 Smart
 - S73000: S7 300 cpu type
 - S7400: S7 400 cpu type
 - S71200: S7 1200 cpu type
 - S71500: S7 1500 cpu type

The <code>TagId</code>s must currently be numbers or be preceded by <code>DB</code>: Currently just Datablocks are supported by this library.

The S7Net library exchanges an important piece of data from the PLC that is the PDU size: by using this information, SmartIOT.Connector is able to maximize the performance of reads and writes, in particular by using the parameters if you enable them in the general device configuration section (see [here](../../Docs/Configuration.md#configuring-the-devices))

<pre>
"IsPartialReadsEnabled": true,
"IsWriteOptimizationEnabled": true,
</pre>
