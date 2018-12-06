FROM microsoft/dotnet:2.1-runtime AS base
ENV ROKU_RELAY_SERIALNUMBER="<SERIAL NUMBER>"
ENV ROKU_RELAY_CONNECTIONSTRING=="<CONNECTION STRING>"
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
ENTRYPOINT exec dotnet RokuDotNet.Relay.dll listen -s $ROKU_RELAY_SERIALNUMBER -c $ROKU_RELAY_CONNECTIONSTRING
