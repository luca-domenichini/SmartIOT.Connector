# SmartIOT.Connector.ConsoleApp

This project implements a simple SmartIOT.Connector runner able to run a SmartIOT.Connector instance as a console application.

The main program takes a json configuration file as parameter, or uses the default <code>./smartiot-config.json</code> if none is provided.
See [this file](./smartiot-config.json) for a sample configuration. Please note that the internal <code>"Configuration"</code> element reflects the Core SmartIOT.Connector configuration (see [here](../../Docs/Configuration.md))

## Docker support

The Console runner can also be run on Docker. You just need to map a volume pointing the configuration folder <code>/SmartIOT.Connector</code> and expose the ports needed for external communications.<br>
The container image will look for configuration file at <code>/SmartIOT.Connector/smartiot-config.json</code>

A prebuilt Docker image is also available on Docker Hub. Use the following command to pull the latest image, or browse https://hub.docker.com/repository/docker/lucadomenichini/smartiot-connector-consoleapp for available tags.

<pre>docker pull lucadomenichini/smartiot-connector-consoleapp:latest</pre>

### Building the Docker image from source

Provided you installed docker on your machine, go to SmartIOT.Connector root project folder and type:

<pre>docker build -t smartiot-connector -f Apps/SmartIOT.Connector.ConsoleApp/Dockerfile .</pre>

### Running the container

To run the container you need to provide the volume where the runner will search the <code>smartiot-config.json</code> configuration file. By default, the container will look for <code>/SmartIOT.Connector/smartiot-config.json</code> file.

You will also need to expose the ports where the communications with external containers happens. Suppose you are running a Mqtt Server inside the container on port 1883, and exposing Prometheus metrics on port 9001 (as per [sample configuration file](./smartiot-config.json))

Type this to run the container and expose ports on the host machine:
<pre>
docker run -it --rm -v /path/to/smartiot-connector/configuration/folder:/SmartIOT.Connector -p 9001:9001 -p 1883:1883 smartiot-connector
</pre>

Suppose you have downloaded the solution on Windows on folder C:\develop\SmartIOT.Connector. You will have an <code>smartiot-config.json</code> file under the <code>SmartIOT.Connector.ConsoleApp</code> project folder.<br>
Type this to use that configuration file:<pre>
docker run -it --rm -v C:\develop\SmartIOT.Connector\Apps\SmartIOT.Connector.ConsoleApp:/SmartIOT.Connector -p 9001:9001 -p 1883:1883 smartiot-connector</pre>
