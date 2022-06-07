# Mqtt connector

This connector exposes events published by SmartIOT.Connector in form of messages sent over Mqtt protocol.<br/>
The connector comes in two flavors: Mqtt Client and Mqtt Server. Users can choose whichever suits most their needs.

The key difference between the two kind of connectors is that Mqtt Client always publishes all the data read from the Tags, even if there is just one byte that changed its value.<br/>
Contrarily, The Mqtt Server is able to publish just the chunks of bytes that changed their values from the last schedule, allowing for a more performant solution. Whenever a client connects to the server, it is initialized by receiving all the Tags handled by the connector with an "initialization message". From then on, the client will just receive the changing bytes.

See project [SmartIOT.Connector.Messages](../../Core/SmartIOT.Connector.Messages/README.md) for details about messages exchanged over the wire.

## Mqtt Client connector configuration

As said before, every event published by Mqtt Client connector is always a full Tag data event. That means that for large data you will probably find a better suit with the Mqtt Server connector, because it is able to publish just the changed chunks.

The Mqtt Client connector accepts a connection string in this form (items in square parenthesis are optional; values indicated here are the defaults):

<pre>mqttClient://Server=<hostname or ip>;Port=<server port>[;ClientId=GUID][;ExceptionTopic=exceptions][;DeviceStatusEventsTopic=deviceStatus/device\${DeviceId}][;TagReadEventsTopic=tagRead/device\${DeviceId}/tag\${TagId}][;TagWriteRequestCommandsTopicRoot=tagWrite][;PublishWriteEvents=false][;Serializer=json][;Username=myUser][;Password=passw0rd]</pre>

Note that inside the topic patterns, there are special values that are replaced at runtime: <code>\${DeviceId}</code> and <code>\${TagId}</code>

The currently available serializers are <code>json</code> and <code>protobuf</code>.

Examples:<pre>
mqttClient://Server=192.168.0.100;Port=1883
mqttClient://Server={your hub name}.azure-devices.net;Port=8883;Username={your hub name}.azure-devices.net/MyDevice01/?api-version=2021-04-12;Password=SharedAccessSignature sig={signature-string}&se={expiry}&sr={URL-encoded-resourceURI}
</pre>

See here for more information about Azure IoT authentication https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-mqtt-support

## Mqtt Server connector configuration

The Mqtt Server connector creates a local mqtt server that accepts client connections and publishes events to specific topics. It also listen to a topic for incoming write requests.

The Mqtt Server connector accepts a connection string in this form (items in square parenthesis are optional; values indicated here are the defaults):

<pre>mqttServer://ServerId=<server identifier>;Port=<server port>[;ExceptionTopic=exceptions][;DeviceStatusEventsTopic=deviceStatus/device\${DeviceId}][;TagReadEventsTopic=tagRead/device\${DeviceId}/tag\${TagId}][;TagWriteRequestCommandsTopicRoot=tagWrite][;PublishWriteEvents=false][;Serializer=json]</pre>

Note that inside the topic patterns, there are special values that are replaced at runtime: <code>\${DeviceId}</code> and <code>\${TagId}</code>

The currently available serializers are <code>json</code> and <code>protobuf</code>.

Examples:<pre>
mqttServer://ServerId=MyServer;Port=1883
mqttServer://ServerId=MyServer;Port=1883;Serializer=protobuf</pre>

