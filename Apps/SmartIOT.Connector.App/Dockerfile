FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build-env
WORKDIR /build

# used for versioning the dll
ARG version

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore SmartIOT.Connector.App.sln

RUN dotnet build --no-restore -c Release SmartIOT.Connector.App.sln

RUN dotnet publish -c Release -o out -f net8.0 /p:version=$version SmartIOT.Connector.App.sln

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build-env /build/out .
ENTRYPOINT ["dotnet", "SmartIOT.Connector.App.dll", "/SmartIOT.Connector/smartiot-config.json"]
