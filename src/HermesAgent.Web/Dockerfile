FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY HermesAgent.sln .
COPY src/HermesAgent.Core/HermesAgent.Core.csproj           src/HermesAgent.Core/
COPY src/HermesAgent.Agent/HermesAgent.Agent.csproj         src/HermesAgent.Agent/
COPY src/HermesAgent.Tools/HermesAgent.Tools.csproj         src/HermesAgent.Tools/
COPY src/HermesAgent.Skills/HermesAgent.Skills.csproj       src/HermesAgent.Skills/
COPY src/HermesAgent.Memory/HermesAgent.Memory.csproj       src/HermesAgent.Memory/
COPY src/HermesAgent.Web/HermesAgent.Web.csproj             src/HermesAgent.Web/

RUN dotnet restore src/HermesAgent.Web/HermesAgent.Web.csproj

COPY . .
RUN dotnet publish src/HermesAgent.Web/HermesAgent.Web.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN adduser --disabled-password --gecos '' hermes && mkdir -p /data && chown hermes /data
USER hermes
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["./hermes-web"]
