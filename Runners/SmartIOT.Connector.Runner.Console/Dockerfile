FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build-env
WORKDIR /build

# Copy everything
COPY . ./
# Restore as distinct layers
RUN dotnet restore SmartIOT.Connector.Runner.Console.sln
# Build and publish a release
RUN dotnet publish SmartIOT.Connector.Runner.Console.sln -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:6.0
WORKDIR /app
COPY --from=build-env /build/out .
ENTRYPOINT ["dotnet", "SmartIOT.Connector.Runner.Console.dll", "/SmartIOT.Connector/smartiot-config.json"]
