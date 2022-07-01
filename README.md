[![.NET](https://github.com/luca-domenichini/SmartIOT.Connector/actions/workflows/dotnet-release.yml/badge.svg?branch=release)](https://github.com/luca-domenichini/SmartIOT.Connector/actions/workflows/dotnet-release.yml)
[![NuGet version (SmartIOT.Connector)](https://img.shields.io/nuget/v/SmartIOT.Connector.Core.svg?style=flat-square)](https://www.nuget.org/packages/SmartIOT.Connector.Core/)

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
For message formats, read the docs [here for project SmartIOT.Connector.Messages](./Core/SmartIOT.Connector.Messages/README.md). JSON serializer is used by default, but Protobuf can also be used, or even your own serializer.

1. Create a configuration json file (see [this file](./Docs/Configuration.md) for configuration reference):
```json
{
	"ConnectorConnectionStrings": [
		"mqttClient://Server=<IpAddress or hostname>;ClientId=MyClient;Port=1883"
	],
	"DeviceConfigurations": [
		{
			"ConnectionString": "snap7://Ip=<IpAddress>;Rack=0;Slot=0;Type=PG",
			"DeviceId": "1",
			"Enabled": true,
			"Name": "Test Device",
			"IsPartialReadsEnabled": false,
			"IsWriteOptimizationEnabled": true,
			"Tags": [
				{
					"TagId": "DB20",
					"TagType": "READ",
					"ByteOffset": 0,
					"Size": 100,
					"Weight": 1
				},
				{
					"TagId": "DB22",
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

```csharp
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
 - [Connectors guide](./Docs/Configuration.md#configuring-the-connectors)
 - [Device configuration guide](./Docs/Configuration.md#configuring-the-devices)
	- [Snap7 PLC configuration guide](./Devices/SmartIOT.Connector.Plc.Snap7/README.md)
	- [S7Net PLC configuration guide](./Devices/SmartIOT.Connector.Plc.S7Net/README.md)
 - [Scheduler configuration guide](./Docs/Configuration.md#configuring-the-scheduler-main-properties)
 - [Connectors guide](./Docs/Connectors.md)
 - [Customization guide](./Docs/Customize.md)

## SmartIOT.Connector.ConsoleApp and Docker integration

If you want to run SmartIOT.Connector as a standalone application or as a Docker container, see project [SmartIOT.Connector.ConsoleApp](./Runners/SmartIOT.Connector.ConsoleApp/README.md) for further details.

Here is a quick link to the Docker image repository: https://hub.docker.com/repository/docker/lucadomenichini/smartiot-connector-consoleapp

## Nuget packages

You can find SmartIOT.Connector packages on nuget.org site and on Visual Studio Package Manager:
https://www.nuget.org/packages?q=SmartIOT.Connector

## Credits

Currently Siemens PLCs support is provided by Snap7 library (http://snap7.sourceforge.net/) and S7Net library (https://github.com/S7NetPlus/s7netplus), so the same PLCs families supported by those libraries are also supported here.

## Disclaimer

As of version 0.x, interfaces and implementation details are subject to change without notice.
I will do my best to keep the interfaces stable, but there are possibilities to incur in such breaking changes.

**currently Siemens PLCs are the only supported devices

## Roadmap to 1.0 - Features TODO list:

 - [ ] REST Api Connector (included in default CosoleApp project)
 - [ ] GRPC Server Connector
 - [X] TCP Server Connector
 - [X] TCP Client Connector
 - [ ] Update docs for connectors: some parameters are not documented
 - [ ] Web app with monitoring capabilities (included in default ConsoleApp project)
 - [ ] Extensibility docs
   - [ ] How to create and plug a custom device
   - [ ] How to create and plug a custom connector
 - [X] Nuget packages on nuget.org - https://www.nuget.org/packages?q=SmartIOT.Connector
 - [X] Docker runner image on dockerhub - https://hub.docker.com/repository/docker/lucadomenichini/smartiot-connector-consoleapp
 - [ ] Runners
   - [X] Run SmartIOT.Connector as a console app
   - [X] Run SmartIOT.Connector as a Docker image
   - [ ] Run SmartIOT.Connector as a WPF app
   - [ ] Run SmartIOT.Connector as a WinService
 - [ ] Testers: connector counterpart as WPF app
   - [ ] REST Api Client
   - [ ] GRPC Client
   - [ ] TCP client
   - [ ] TCP server

## Technical TODO list

 - [ ] Leverage the async pattern for Connectors and Devices:
	   introduce <code>IAsyncDeviceDriver</code> and <code>IAsyncConnector</code> and add support to autodiscover and run them
 - [ ] The proto files should be part of SmartIOT.Connector.Messages project
 - [ ] Introduce tag free parameters string in TagConfiguration
 - [ ] ConsoleApp
   - [ ] Customizable logs from json config
   - [ ] Log to file
