# Connectors guide

This page provides information on how to setup a connector for SmartIOT.Connector and how to write your own.

A connector is a component of SmartIOT.Connector that allows you to publish the information obtained from a device to an external receiver in form of an event.
Such receiver can then elaborate on those events and perform application logic to generate write requests to SmartIOT.Connector. Those requests are then propagated to the underlying devices and written to them as requested.

Every connector should publish the messages contained in project [SmartIOT.Connector.Messages](../Core/SmartIOT.Connector.Messages/README.md) serialized in any form suitable for that specific connector. Two kind of serialization are provided out of the box: Json and Protobuf.

These are the connectors provided by SmartIOT.Connector out of the box:

 - [Mqtt Connector](../Connectors/SmartIOT.Connector.Mqtt/README.md):
  This connector uses Mqtt protocol to communicate SmartIOT.Connector events with an external system. The connector comes in two flavors: [MqttClientConnector](../Connectors/SmartIOT.Connector.Mqtt/Client/MqttClientConnector.cs) and [MqttServerConnector](../Connectors/SmartIOT.Connector.Mqtt/Server/MqttServerConnector.cs).
  
 - [Tcp Connector](../Connectors/SmartIOT.Connector.Tcp/README.md):
  This connector uses raw sockets to publish SmartIOT.Connector events with an external system. The connector comes in two flavors: [TcpClientConnector](../Connectors/SmartIOT.Connector.Tcp/Client/TcpClientConnector.cs) and [TcpServerConnector](../Connectors/SmartIOT.Connector.Tcp/Server/TcpServerConnector.cs).
