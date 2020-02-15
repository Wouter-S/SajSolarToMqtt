
FROM mcr.microsoft.com/dotnet/core/aspnet:3.0-bionic-arm64v8 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/core/sdk:3.0-bionic AS build
WORKDIR /src
COPY ["SajSolarToMqtt.csproj", ""]
RUN dotnet restore "./SajSolarToMqtt.csproj"
COPY . .
WORKDIR "/src/."
RUN dotnet build "SajSolarToMqtt.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SajSolarToMqtt.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SajSolarToMqtt.dll"]