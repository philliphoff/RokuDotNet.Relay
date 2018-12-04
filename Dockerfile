FROM microsoft/dotnet:2.1-runtime AS base
WORKDIR /app

FROM microsoft/dotnet:2.1-sdk AS build
WORKDIR /src
COPY ["src/RokuDotNet.Relay.csproj", "src/"]
RUN dotnet restore "src/RokuDotNet.Relay.csproj"
COPY . .
WORKDIR "/src/src"
RUN dotnet build "RokuDotNet.Relay.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "RokuDotNet.Relay.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "RokuDotNet.Relay.dll"]