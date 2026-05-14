# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy solution and project files first for layer caching
COPY HermesAgent.sln .
COPY src/HermesAgent.Core/HermesAgent.Core.csproj src/HermesAgent.Core/
COPY src/HermesAgent.Agent/HermesAgent.Agent.csproj src/HermesAgent.Agent/
COPY src/HermesAgent.Tools/HermesAgent.Tools.csproj src/HermesAgent.Tools/
COPY src/HermesAgent.Skills/HermesAgent.Skills.csproj src/HermesAgent.Skills/
COPY src/HermesAgent.Memory/HermesAgent.Memory.csproj src/HermesAgent.Memory/
COPY src/HermesAgent.Cli/HermesAgent.Cli.csproj src/HermesAgent.Cli/

RUN dotnet restore

COPY . .
RUN dotnet publish src/HermesAgent.Cli/HermesAgent.Cli.csproj \
    -c Release -o /app/publish \
    --no-restore \
    -p:PublishSingleFile=true \
    -p:SelfContained=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Create hermes user (non-root)
RUN adduser --disabled-password --gecos '' hermes
USER hermes

COPY --from=build /app/publish .

# Hermes data volume
VOLUME ["/home/hermes/.hermes"]
ENV HERMES_DataDirectory=/home/hermes/.hermes

ENTRYPOINT ["./hermes"]
CMD ["chat"]
