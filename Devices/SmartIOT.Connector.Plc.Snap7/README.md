# Snap7 Device configuration

This project provides a bridge to make SmartIOT.Connector able to use the [Snap7 library](http://snap7.sourceforge.net) by Davide Nardella (and in particular Sharp7) to connect to a Siemens PLC and to exchange data defined in its datablocks.

The supported connection string is as follows (square brackets for optional parameters):
<pre>snap7://Ip=&lt;plc ip address>;Rack=&lt;rack>;Slot=&lt;slot>[;Type=&lt;type>]</pre>

The values for <code>Rack</code> and <code>Slot</code> should be taken from the hardware configuration in Simatic Manager.
Usually we have <code>Rack=0;Slot=2</code> for S7300 PLCs and <code>Rack=0;Slot=0</code> for S71500 PLCs. Please refer to the Snap7 library site for more on this.

The [<code>Type</code>](S7ConnectionType.cs) parameter must be one of the three:
 - PG: this kind of connection is used to connect as if SmartIOT.Connector is the Siemens PG
 - OP: this kind of connection is used to connect as if SmartIOT.Connector is an OP panel
 - BASIC: this is the general purpose connection type (and is the default)

You should choose the right connection type based on the connection resouce slots defined in Simatic Manager for your PLC. Most of the times, <code>BASIC</code> should work just fine.

The <code>TagId</code>s must currently be numbers or be preceded by <code>DB</code>: Currently just Datablocks are supported by this library.

The Snap7 library exchanges an important piece of data from the PLC that is the PDU size: by using this information, SmartIOT.Connector is able to maximize the performance of reads and writes, in particular by using the parameters if you enable them in the general device configuration section (see [here](../../Docs/Configuration.md#configuring-the-devices))

<pre>
"IsPartialReadsEnabled": true,
"IsWriteOptimizationEnabled": true,
</pre>
