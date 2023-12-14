echo off

set LOCAL_IMAGE=smartiot-connector-app
set REMOTE_IMAGE=lucadomenichini/smartiot-connector-app
set /P TAG_NAME="Enter the tag name to assign to image %REMOTE_IMAGE%: "

docker build --build-arg version=%TAG_NAME% -t %LOCAL_IMAGE%:latest -f Apps/SmartIOT.Connector.App/Dockerfile .

docker tag %LOCAL_IMAGE%:latest %REMOTE_IMAGE%:latest
docker tag %LOCAL_IMAGE%:latest %REMOTE_IMAGE%:%TAG_NAME%
