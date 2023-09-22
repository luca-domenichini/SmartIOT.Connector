# Configuration guide

This page shows you how you can configure SmartIOT.Connector with its standard json configuration file.

The configuration file is composed of 3 main sections:

```
{
  "ConnectorConnectionStrings": [
	...
  ],
  "DeviceConfigurations": [
      ...
  ],
  "SchedulerConfiguration": {
      ...
  }
}
```

[This is the class](../Core/SmartIOT.Connector.Core/SmartIotConnectorConfiguration.cs) that is backing the configuration.

## Configuring the Connectors

The "ConnectorConnectionStrings" section is an array of strings, each one representing a specific Connector attached to an SmartIOT.Connector instance.
Every connectiong string must be in the form ```"identifier://key1=value1;key2=value2;..."```.
Each Connector provides its own specific syntax and logic used to parse the parameters provided in the connectiong string.

A SmartIOT.Connector instance can have multiple Connectors: every event received from the devices will be forwarded to each Connector.

The Connectors guide can be found [here](Connectors.md)

## Configuring the devices

The "DeviceConfigurations" section is an array of [DeviceConfiguration](../Core/SmartIOT.Connector.Core/Conf/DeviceConfiguration.cs).
Each DeviceConfiguration provides information needed to connect to that specific device and which data SmartIOT.Connector should try to read and write to the device.

Each device configuration object is composed like this:
```
    {
        "ConnectionString": "indentifer://key1=value1;key2=value2;...",
        "DeviceId": 1,
        "Enabled": true,
        "Name": "This is a description",
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
            },
            ...
        ]
    }
```

<code>ConnectionString</code><br>
This parameter is the only parameter that is specific to the kind of device you are trying to connect.
This connection string must provide the informations needed to connect to the device and other device-specific settings.
E.g. a remote device will probably have an "Ip" or "IpAddress" key used to reach it through the network.<br>
Please refer to a specific device library project to configure the connection string properly.

<code>DeviceId</code><br>
This is the device identifier and must be unique across the configuration

<code>Enabled</code><br>
This flag can be used to disable a device without deleting it from configuration. A disabled device will be ignored.

<code>Name</code><br>
This is a description of the device

<code>IsPartialReadsEnabled</code><br>
When this flag is enabled, the scheduler will attempt to schedule a Tag-Write in the middle of a read cycle. Set it to true if you have big read Tags and want to boost the performance of tag writes.

<code>IsWriteOptimizationEnabled</code><br>
When this flag is enabled, the scheduler will write to tags just the portion of the data that has been changed from last write. Enable it to boost the write performance.

<code>Tags</code><br>
Each tag is configured is its own section. A Tag can be just for READ or just for WRITE.<br>
&emsp;<code>TagId</code>: tag identifier, unique across a single device. Can be anything that is interpretable by the device itself.<br>
&emsp;<code>TagType</code>: READ or WRITE<br>
&emsp;<code>ByteOffset</code>: Start byte where the Tag begins. This an absolute offset starting from zero.<br>
&emsp;<code>Size</code>: Size in bytes<br>
&emsp;<code>Weight</code>: This field defines how much heavy is a Tag to schedule. A heavier Tag is scheduled less often. E.g. if you confiure 2 tags, the first with Weight=1 and the second with Weight=2, the first tag will be scheduled exactly twice times the second.

## Configuring the scheduler main properties

This section provides basic and general information to SmartIOT.Connector. These informations affects all the schedulers running on that instance.

This section is an object that provides the following informations ([here](../Core/SmartIOT.Connector.Core/Conf/SchedulerConfiguration.cs) the backing class):
```
	"SchedulerConfiguration": {
		"MaxErrorsBeforeReconnection": 10,
		"RestartDeviceInErrorTimeoutMillis": 30000,
		"WaitTimeAfterErrorMillis": 1000,
		"WaitTimeBetweenEveryScheduleMillis": 0,
		"WaitTimeBetweenReadSchedulesMillis": 0,
		"TerminateAfterNoWriteRequestsDelayMillis": 3000,
		"TerminateMinimumDelayMillis": 0
	}
```

```MaxErrorsBeforeReconnection```: This key is used to determine if a device is no more reachable. After this number of consecutive errors, the device is considered dead and a new stop/start cycle is initiated.

```RestartDeviceInErrorTimeoutMillis```: This key is the time in milliseconds to wait before attempting a restart cycle when a device becomes no more reachable.

```WaitTimeAfterErrorMillis```: This key is the time in milliseconds to wait to schedule again a tag that encountered an error while reading or writing to it.

```WaitTimeBetweenEveryScheduleMillis```: This key is the time in milliseconds to wait between every schedule between every tag defined in a scheduler (read or write).

```WaitTimeBetweenReadSchedulesMillis```: This key is the time in milliseconds to wait between every read schedule for a single tag.

```TerminateAfterNoWriteRequestsDelayMillis```: This key is the minimum time in milliseconds to wait before terminating the scheduler after receiving the last write request from any connector.

```TerminateMinimumDelayMillis```: This is key is the minimum time to wait in milliseconds to wait before terminating the scheduler when requested, despite any write request incoming or not.
