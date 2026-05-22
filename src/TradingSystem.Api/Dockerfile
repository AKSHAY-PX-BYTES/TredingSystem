# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy both projects (context is parent 'src/' directory)
COPY TradingSystem.Agent/*.csproj TradingSystem.Agent/
COPY TradingSystem.Api/*.csproj TradingSystem.Api/
RUN dotnet restore TradingSystem.Api/TradingSystem.Api.csproj

COPY TradingSystem.Agent/ TradingSystem.Agent/
COPY TradingSystem.Api/ TradingSystem.Api/

WORKDIR /src/TradingSystem.Api
RUN dotnet publish -c Release -o /app/publish

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:10000
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 10000

ENTRYPOINT ["dotnet", "TradingSystem.Api.dll"]
