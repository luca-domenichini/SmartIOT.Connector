# Tcp connector

This connector exposes events published by SmartIOT.Connector in form of messages sent over Tcp protocol.<br/>
The connector comes in two flavors: Tcp Client and Tcp Server. Users can choose whichever suits most their needs.

Both connectors are able to publish just the chunk of bytes that changed their values from the last schedule, so this connector is especially suited for systems where high performance is a need.
The key difference between the two kind of connectors is that TcpClientConnector connects just to one tcp server, while TcpServerConnector accetps tcp client connections and is able to serve a multitute of clients.<br/>
Whenever the TcpConnector connects to the external system, it is initialized by receiving all the Tags handled by the connector with an "initialization message". From then on, the connector will just publish the changing bytes.

See project [SmartIOT.Connector.Messages](../../Core/SmartIOT.Connector.Messages/README.md) for details about messages exchanged over the wire.

## Tcp Client connector configuration

The TcpClientConnector accepts a connection string in this form (items in square parenthesis are optional; values indicated here are the defaults):

<pre>tcpClient://Server=<hostname or ip>;Port=<server port>[;PublishWriteEvents=false][;Serializer=protobuf][;PingIntervalMillis=0][;ReconnectIntervalMillis=5000]</pre>

The currently available serializers are <code>json</code> and <code>protobuf</code>. Note that the default serializer is Protobuf.

<code>PingIntervalMillis</code>: This parameter sets a recurring interval in milliseconds to send a [PingMessage](../../Core/SmartIOT.Connector.Messages/PingMessage.cs)
<code>ReconnectIntervalMillis</code>: This parameter sets the interval used to retry to connect to the tcp server

Examples:<pre>
tcpClient://Server=192.168.0.100;Port=1883
tcpClient://Server=192.168.0.100;Port=1883;Serializer=json;PingIntervalMillis=10000;ReconnectIntervalMillis=30000
</pre>

## Tcp Server connector configuration

The TcpServerConnector creates a local tcp server that accepts client connections and publishes events to connected clients. It also listen for incoming messages for incoming write requests.

The TcpServerConnector accepts a connection string in this form (items in square parenthesis are optional; values indicated here are the defaults):

<pre>tcpServer://Port=<server port>[;PublishWriteEvents=false][;Serializer=protobuf][;PingIntervalMillis=0]</pre>

The currently available serializers are <code>json</code> and <code>protobuf</code>. Note that the default serializer is Protobuf.

Examples:<pre>
tcpServer://Port=1883
tcpServer://Port=1883;Serializer=json</pre>

