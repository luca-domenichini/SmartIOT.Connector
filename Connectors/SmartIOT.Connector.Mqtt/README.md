# Mqtt connector

This connector exposes events published by SmartIOT.Connector in form of messages sent over Mqtt protocol.<br/>
The connector comes in two flavors: [Mqtt Client](../SmartIOT.Connector.Mqtt/Client/MqttClientConnector.cs) and [Mqtt Server](../SmartIOT.Connector.Mqtt/Server/MqttServerConnector.cs). Users can choose whichever suits most their needs.

The key difference between the two kind of connectors is that Mqtt Client always publishes all the data read from the Tags, even if there is just one byte that changed its value.<br/>
Contrarily, The Mqtt Server is able to publish just the chunks of bytes that changed their values from the last schedule, allowing for a more performant solution. Whenever a client connects to the server, it is initialized by receiving all the Tags handled by the connector with an "initialization message". From then on, the client will just receive the changing bytes.

## Mqtt Client connector configuration

This connector instantiates an mqtt client that connects to a remote mqtt broker. Every event raised by SmartIOT.Connector is then published to the mqtt broker to a corresponding topic. Mqtt client subscribes to a topic to receive write requests from the broker. As said before, every event published by Mqtt Client connector is always a full Tag data event. That means that for large data you will probably find a better suit with the Mqtt Server connector, because it is able to publish just the changed chunks.

The Mqtt Client connector accepts a connection string in this form (items in square parenthesis are optional; values indicated here are the defaults):

<pre>mqttClient://Server=<hostname or ip>;Port=<server port>[;ClientId=GUID][;ExceptionTopic=exceptions][;DeviceStatusEventsTopic=deviceStatus/device\${DeviceId}][;TagReadEventsTopic=tagRead/device\${DeviceId}/tag\${TagId}][;TagWriteRequestCommandsTopicRoot=tagWrite][;PublishWriteEvents=false][;Serializer=json][;Username=][;Password=]</pre>

Note that inside the topic patterns, there are special values that are replaced at runtime: <code>\${DeviceId}</code> and <code>\${TagId}</code>

The currently available serializers are <code>json</code> and <code>protobuf</code>.

Examples:<pre>
mqttClient://Server=192.168.0.100;Port=1883
mqttClient://Server={your hub name}.azure-devices.net;Port=8883;Username={your hub name}.azure-devices.net/MyDevice01/?api-version=2021-04-12;Password=SharedAccessSignature sig={signature-string}&se={expiry}&sr={URL-encoded-resourceURI}
</pre>

See here for more information about Azure IoT authentication https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-mqtt-support

## Mqtt Server connector configuration

The Mqtt Server connector creates a local mqtt server that accepts client connections and publishes events to specific topics. It also listen to a topic for incoming write requests. Every event raised by SmartIOT.Connector is published by the mqtt broker to a corresponding topic, configurable in the connection string.

The Mqtt Server connector accepts a connection string in this form (items in square parenthesis are optional; values indicated here are the defaults):

<pre>mqttServer://ServerId=<server identifier>;Port=<server port>[;ExceptionTopic=exceptions][;DeviceStatusEventsTopic=deviceStatus/device\${DeviceId}][;TagReadEventsTopic=tagRead/device\${DeviceId}/tag\${TagId}][;TagWriteRequestCommandsTopicRoot=tagWrite][;PublishWriteEvents=false][;Serializer=json][;PublishPartialReads=true]</pre>

Note that inside the topic patterns, there are special values that are replaced at runtime: <code>\${DeviceId}</code> and <code>\${TagId}</code>

The currently available serializers are <code>json</code> and <code>protobuf</code>.

<code>PublishPartialReads</code>: This parameter enables/disables the publishing of partial read events. If disabled, the full tag data is always sent.

Examples:<pre>
mqttServer://ServerId=MyServer;Port=1883
mqttServer://ServerId=MyServer;Port=1883;Serializer=protobuf;PublishPartialReads=false</pre>

