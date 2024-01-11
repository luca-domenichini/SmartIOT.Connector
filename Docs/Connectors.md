# Connectors guide

This page provides information on how to setup a connector for SmartIOT.Connector and how to write your own.

A connector is a component of SmartIOT.Connector that allows you to publish the information obtained from a device to an external receiver in form of an event.
Such receiver can then elaborate on those events and perform application logic to generate write requests to SmartIOT.Connector. Those requests are then propagated to the underlying devices and written to them as requested.

Every connector should publish the messages contained in project [SmartIOT.Connector.Messages](../Core/SmartIOT.Connector.Messages/README.md) serialized in any form suitable for that specific connector. Two kind of serialization are provided out of the box: Json and Protobuf.

Two kind of serialization are provided out of the box: single message and stream based.<br>
See below for further details.

These are the connectors provided by SmartIOT.Connector out of the box:

 - [Mqtt Connector](../Connectors/SmartIOT.Connector.Mqtt/README.md):
  This connector uses Mqtt protocol to communicate SmartIOT.Connector events with an external system. The connector comes in two flavors: [MqttClientConnector](../Connectors/SmartIOT.Connector.Mqtt/Client/MqttClientConnector.cs) and [MqttServerConnector](../Connectors/SmartIOT.Connector.Mqtt/Server/MqttServerConnector.cs).
  This connector uses single message serializer.
  
 - [Tcp Connector](../Connectors/SmartIOT.Connector.Tcp/README.md):
  This connector uses raw sockets to publish SmartIOT.Connector events with an external system. The connector comes in two flavors: [TcpClientConnector](../Connectors/SmartIOT.Connector.Tcp/Client/TcpClientConnector.cs) and [TcpServerConnector](../Connectors/SmartIOT.Connector.Tcp/Server/TcpServerConnector.cs).
  This connector uses stream message serializer.

## Serialization of messages

Different connectors can choose the way they serialize messages over the wire. Sometimes the protocol used by the connector is able to delimit the boundary of messages by itself and sometimes it is not.<br>
When the protocol is able to split each message in a sequence of bytes, then we can use the [single message serializer](../Core/SmartIOT.Connector.Messages/Serializers/ISingleMessageSerializer.cs).<br>
When the protocol is not able to split each message, some extra bytes are needed to be sent on the network to mark the message type and/or length. In this case, the [stream message serializer](../Core/SmartIOT.Connector.Messages/Serializers/IStreamMessageSerializer.cs) should be used instead, and extra logic must be put on the connector to decode the protocol and interpret the incoming stream.

### [Single message serializer](../Core/SmartIOT.Connector.Messages/Serializers/ISingleMessageSerializer.cs)

In this case the protocol is able to split each message in single <code>byte[]</code>. If the origin of the message is known, such as a specific topic for MqttConnector, it is enough to deserialize the message with the desired implementation (json, protobuf, ...). Otherwise, it is necessary to determine the type of the message maybe write it on the first byte.

The two provided implementations are:<br>
 - [Json message serializer](../Core/SmartIOT.Connector.Messages/Serializers/JsonSingleMessageSerializer.cs)
 - [Protobuf message serializer](../Core/SmartIOT.Connector.Messages/Serializers/ProtobufSingleMessageSerializer.cs)

### [Stream message serializer](../Core/SmartIOT.Connector.Messages/Serializers/IStreamMessageSerializer.cs)

In case of a stream based serializer, the protocol itself does not split each message in a <code>byte[]</code>, so it is the serializer duty to do that splitting. Moreover, the serializer must also distinguish each message by a byte marker inside the stream, because every message type comes from the same stream.

To determine the type of the message, a single byte prefix is used. These are the values used to discriminate the message type:
 - 1: [TagEvent](../Core/SmartIOT.Connector.Messages/TagEvent.cs)
 - 2: [DeviceEvent](../Core/SmartIOT.Connector.Messages/DeviceEvent.cs)
 - 3: [TagWriteRequestCommand](../Core/SmartIOT.Connector.Messages/TagWriteRequestCommand.cs)
 - 99: [PingMessage](../Core/SmartIOT.Connector.Messages/PingMessage.cs)

The two provided implementations are:<br>
 - [Json stream serializer](../Core/SmartIOT.Connector.Messages/Serializers/JsonStreamMessageSerializer.cs): this serializer splits each message with a line break (\n, 0x0A, 10) (see also [here](https://en.wikipedia.org/wiki/JSON_streaming#Line-delimited_JSON))
 - [Protobuf stream serializer](../Core/SmartIOT.Connector.Messages/Serializers/ProtobufStreamMessageSerializer.cs): this serializer splits each message with the number prefix above, and serializes the length of each message to separate them (see [here](https://eli.thegreenplace.net/2011/08/02/length-prefix-framing-for-protocol-buffers) for details. The length-prefix method is provided by [protobuf-net library](https://github.com/protobuf-net/protobuf-net))
