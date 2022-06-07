echo off

set /P VERSION="Enter the package version to push: "
set /P API_KEY="Enter to API Key to use to push the packages: "

nuget push -Source nuget.org Connectors\SmartIOT.Connector.Mqtt\bin\Debug\SmartIOT.Connector.Mqtt.%VERSION%.nupkg %API_KEY%
nuget push -Source nuget.org Core\SmartIOT.Connector.Core\bin\Debug\SmartIOT.Connector.Core.%VERSION%.nupkg %API_KEY%
nuget push -Source nuget.org Core\SmartIOT.Connector.Messages\bin\Debug\SmartIOT.Connector.Messages.%VERSION%.nupkg %API_KEY%
nuget push -Source nuget.org Core\SmartIOT.Connector.Prometheus\bin\Debug\SmartIOT.Connector.Prometheus.%VERSION%.nupkg %API_KEY%
nuget push -Source nuget.org Devices\SmartIOT.Connector.Plc.S7Net\bin\Debug\SmartIOT.Connector.Plc.S7Net.%VERSION%.nupkg %API_KEY%
nuget push -Source nuget.org Devices\SmartIOT.Connector.Plc.Snap7\bin\Debug\SmartIOT.Connector.Plc.Snap7.%VERSION%.nupkg %API_KEY%
nuget push -Source nuget.org Runners\SmartIOT.Connector.Runner.Console\bin\Debug\SmartIOT.Connector.Runner.Console.%VERSION%.nupkg %API_KEY%
