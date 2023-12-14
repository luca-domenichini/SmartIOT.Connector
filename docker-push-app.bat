echo off

set REMOTE_IMAGE=lucadomenichini/smartiot-connector-app
set /P TAG_NAME="Enter the tag name to push for image %REMOTE_IMAGE%: "

docker push %REMOTE_IMAGE%:%TAG_NAME%
docker push %REMOTE_IMAGE%:latest
