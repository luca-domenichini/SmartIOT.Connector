# SmartIOT.Connector - Cloud Connector for IOT devices and industrial PLCs

This project aims at creating a simple connector and scheduler for automation devices, like industrial PLCs, publishing data to the cloud and more.

![SmartIOT.Connector image](Docs/smartiot-connector.jpg)

SmartIOT.Connector enables you to connect to a variety of IOT sensors and industrial PLCs** and distribute their data to an external system in form of an event describing the changed data.<br>
The external system can then process the data being read and can send back to SmartIOT.Connector data to be written to the devices.<br>
SmartIOT.Connector is a good fit for industrial and automation needs, where developers are being asked to abstract away from device communication protocols and must concentrate solely on the business logic to implement.

## Quick start

The following quick start creates an SmartIOT.Connector instance that connects to a device (namely a Siemens PLC) and reads 100 bytes from Tag 20.<br/>
Whenever a change is detected in the tag, a message is published to the Mqtt Server specified in the connection string below.<br/>
It also listens for incoming messages in topic <code>tagWrite</code> and tries to write data to tag 22.<br/>
For message formats, read the docs [here for project SmartIOT.Connector.Messages](./Core/SmartIOT.Connector.Messages/README.md). [JSON serializer](./Connectors/SmartIOT.Connector.Mqtt/JsonMessageSerializer.cs) is used by default, but [Protobuf](./Connectors/SmartIOT.Connector.Mqtt/ProtobufMessageSerializer.cs) can also be used, or even your own serializer.

1. Create a configuration json file (see [this file](./Docs/Configuration.md) for configuration reference):
```
{
	"ConnectorConnectionStrings": [
		"mqttClient://Server=<IpAddress or hostname>;ClientId=MyClient;Port=1883"
	],
	"DeviceConfigurations": [
		{
			"ConnectionString": "snap7://Ip=<IpAddress>;Rack=0;Slot=0;Type=PG",
			"DeviceId": 1,
			"Enabled": true,
			"Name": "Test Device",
			"IsPartialReadsEnabled": false,
			"IsWriteOptimizationEnabled": true,
			"Tags": [
				{
					"TagId": 20,
					"TagType": "READ",
					"ByteOffset": 0,
					"Size": 100,
					"Weight": 1
				},
				{
					"TagId": 22,
					"TagType": "WRITE",
					"ByteOffset": 0,
					"Size": 100,
					"Weight": 1
				}
			]
		}
	],
	"SchedulerConfiguration": {
		"MaxErrorsBeforeReconnection": 10,
		"RestartDeviceInErrorTimeoutMillis": 30000,
		"WaitTimeAfterErrorMillis": 1000,
		"WaitTimeBetweenEveryScheduleMillis": 0,
		"WaitTimeBetweenReadSchedulesMillis": 0
	}
}
```

2. Use SmartIotConnectorBuilder to create the connector and run it:

```
// Build SmartIOT.Connector and bind it to your DI container or wherever you can do this:
var smartiot = new SmartIOT.Connector.Core.SmartIotConnectorBuilder()
	.WithAutoDiscoverDeviceDriverFactories()
	.WithAutoDiscoverConnectorFactories()
	.WithConfigurationJsonFilePath("smartiot-config.json")
	.Build();

// Start SmartIOT.Connector whenever you need it to run
smartiot.Start();

// Stop SmartIOT.Connector before shutting down everything
smartiot.Stop();
```

Follow the [configuration guide](./Docs/Configuration.md) to get a complete understanding of how the configuration works.
You can even jump to section specific guides:
 - [Connectors guide](./Docs/Connectors.md)
	- [Mqtt Connector guide](./Connectors/SmartIOT.Connector.Mqtt/README.md)
 - [Device configuration guide](./Docs/Configuration.md#configuring-the-devices)
	- [Snap7 PLC configuration guide](./Devices/SmartIOT.Connector.Plc.Snap7/README.md)
	- [S7Net PLC configuration guide](./Devices/SmartIOT.Connector.Plc.S7Net/README.md)

## SmartIOT.Connector.Runner.Console and Docker integration

If you want to run SmartIOT.Connector as a standalone application or as a Docker container, see project [SmartIOT.Connector.Runner.Console](./Runners/SmartIOT.Connector.Runner.Console/README.md) for further details.

## Nuget packages

You can find SmartIOT.Connector packages on nuget.org site and on Visual Studio Package Manager

## Credits

Currently Siemens PLCs support is provided by Snap7 library (http://snap7.sourceforge.net/) and S7Net library (https://github.com/S7NetPlus/s7netplus), so the same PLCs families supported by those libraries are also supported here.


**currently Siemens PLCs is the only supported device

## Features TODO list:

 - REST Api Connector
 - GRPC Server Connector
 - Protobuf on TCP Server Connector
 - Protobuf on TCP Client Connector
 - [OK] Nuget packages on nuget.org
 - [OK] Docker runner image on dockerhub https://hub.docker.com/repository/docker/lucadomenichini/smartiot-connector-runner-console
 - Runners
   - [OK] Run SmartIOT.Connector as a console app
   - [OK] Run SmartIOT.Connector as a Docker image
   - Run SmartIOT.Connector as a WPF app
   - Run SmartIOT.Connector as a WinService
 - Testers: connector counterpart as WPF app
   - REST Api Client
   - GRPC Client
   - TCP client
   - TCP server

## Technical TODO list

 - Leverage the async pattern for Connectors
 - The proto files should be part of SmartIOT.Connector.Messages project
