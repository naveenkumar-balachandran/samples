# Multi-stage Dockerfile for ASP.NET Core 8.0 app

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY . .
RUN dotnet restore ./IframeFullServer.csproj \
    && dotnet publish ./IframeFullServer.csproj -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# ASP.NET Core Kestrel ports
EXPOSE 8080
EXPOSE 8443

# Map Kestrel to 8080 by default
ENV ASPNETCORE_URLS=http://+:8080

# Forwarded headers are handled in Program.cs
ENTRYPOINT ["dotnet", "IframeFullServer.dll"]
