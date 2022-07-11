echo off

rem clear previous suffix value
set VERSION_PREFIX=0.0.%date:~-2%%date:~3,2%%date:~0,2%%time:~0,2%
set VERSION_SUFFIX=

set /P RELEASE="Enter the configuration to build and release [Debug|Release]: "
set /P VERSION_SUFFIX="Enter a version suffix or blank (e.g. -beta): "
set /P PUSH_NUGET_PACKAGES="Do you want to push the nuget packages to nuget.org? [y|n]: "
if /I "%PUSH_NUGET_PACKAGES%"=="y" (
    set /P API_KEY="Enter to API Key to use to push the packages: "
)
set /P PUSH_DOCKER_IMAGE="Do you want to build and publish the SmartIOT.Connector.ConsoleApp Docker Image? [y|n]: "

set VERSION=%VERSION_PREFIX%%VERSION_SUFFIX%

echo ----------------------------------------------------
echo Using version: %VERSION%
echo Cleaning solution
echo ----------------------------------------------------
dotnet clean -c %RELEASE% SmartIOT.Connector.sln

echo ----------------------------------------------------
echo Restoring solution
echo ----------------------------------------------------
dotnet restore SmartIOT.Connector.sln
if errorlevel 1 (
    echo Restore failed: %errorlevel%
    exit /b %errorlevel%
)

echo ----------------------------------------------------
echo Building solution
echo ----------------------------------------------------
dotnet build --no-restore -c %RELEASE% SmartIOT.Connector.sln
if errorlevel 1 (
    echo Build failed: %errorlevel%
    exit /b %errorlevel%
)

echo ----------------------------------------------------
echo Testing solution
echo ----------------------------------------------------
dotnet test --no-build -c %RELEASE% SmartIOT.Connector.sln
if errorlevel 1 (
    echo Testing failed: %errorlevel%
    exit /b %errorlevel%
)

if /I "%PUSH_NUGET_PACKAGES%" == "y" (

    echo ----------------------------------------------------
    echo Building packages
    echo ----------------------------------------------------
    dotnet pack -c %RELEASE% -p:VersionPrefix=%VERSION_PREFIX% -p:VersionSuffix=%VERSION_SUFFIX% SmartIOT.Connector.sln
    if errorlevel 1 (
        echo Packing failed: %errorlevel%
        exit /b %errorlevel%
    )

    echo ----------------------------------------------------
    echo Publishing packages, version %VERSION%
    echo ----------------------------------------------------
    nuget push -Source nuget.org Connectors\SmartIOT.Connector.Mqtt\bin\%RELEASE%\SmartIOT.Connector.Mqtt.%VERSION%.nupkg %API_KEY%
    nuget push -Source nuget.org Core\SmartIOT.Connector.Core\bin\%RELEASE%\SmartIOT.Connector.Core.%VERSION%.nupkg %API_KEY%
    nuget push -Source nuget.org Core\SmartIOT.Connector.Messages\bin\%RELEASE%\SmartIOT.Connector.Messages.%VERSION%.nupkg %API_KEY%
    nuget push -Source nuget.org Core\SmartIOT.Connector.Prometheus\bin\%RELEASE%\SmartIOT.Connector.Prometheus.%VERSION%.nupkg %API_KEY%
    nuget push -Source nuget.org Devices\SmartIOT.Connector.Plc.S7Net\bin\%RELEASE%\SmartIOT.Connector.Plc.S7Net.%VERSION%.nupkg %API_KEY%
    nuget push -Source nuget.org Devices\SmartIOT.Connector.Plc.Snap7\bin\%RELEASE%\SmartIOT.Connector.Plc.Snap7.%VERSION%.nupkg %API_KEY%
    nuget push -Source nuget.org Apps\SmartIOT.Connector.ConsoleApp\bin\%RELEASE%\SmartIOT.Connector.ConsoleApp.%VERSION%.nupkg %API_KEY%
)

if /I "%PUSH_DOCKER_IMAGE%" == "y" (

    echo -------------------------------------------------------
    echo Building SmartIOT.Connector.ConsoleApp Docker Image
    echo -------------------------------------------------------

    set LOCAL_IMAGE=smartiot-connector-consoleapp
    set REMOTE_IMAGE=lucadomenichini/smartiot-connector-consoleapp

    docker build -t %LOCAL_IMAGE%:latest -f Apps/SmartIOT.Connector.ConsoleApp/Dockerfile .
    if errorlevel 1 (
        echo Error building Docker Image: %errorlevel%
        exit /b %errorlevel%
    )

    docker tag %LOCAL_IMAGE%:latest %REMOTE_IMAGE%:latest
    docker tag %LOCAL_IMAGE%:latest %REMOTE_IMAGE%:%VERSION%


    echo ------------------------------------------------------
    echo Pushing SmartIOT.Connector.ConsoleApp Docker Image
    echo ------------------------------------------------------

    docker push %REMOTE_IMAGE%:%VERSION%
    docker push %REMOTE_IMAGE%:latest
)
