# SmartIOT.Connector Messages

This project defines the message format that connectors should use to communicate to external systems the events published by SmartIOT.Connector.

The messages are normally serialized is json format or in binary format, using Protobug seralizer.

## [DeviceEvent](./DeviceEvent.cs)

This event is used to communicate to external systems a new device status detected. The event carries information about the error code encoutered and an information string.

## [TagEvent](./TagEvent.cs)

This event is used to communicate a tag event. A tag event can carry data with it or it can be used to communicate just a tag status: in that case, the bytes within the message will be NULL. A tag status typically indicates some problem when reading or writing data to a specific tag, e.g. the tag does not exists or it has the wrong size.

## [ExceptionEvent](./ExceptionEvent.cs)

This event is used to communicate an exception encoutered inside SmartIOT.Connector. This event should be used to log the exception somewhere useful.

## [TagWriteRequestCommand](./TagWriteRequestCommand.cs)

This is the sole command that SmartIOT.Connector is able to process. This command is read by the SmartIOT.Connector Connectors and is interpreted as a request to write some data to a specific tag. The command should contain the bytes to write at a specific offset inside the tag. Each Connector provides its own way to receive this command: e.g. the Mqtt Connector listens to a specific topic for this kind of message (see [here](../../Connectors/SmartIOT.Connector.Mqtt/README.md))
