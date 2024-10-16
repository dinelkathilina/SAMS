# Use the official .NET 8 SDK image for building
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["SAMS/SAMS.csproj", "SAMS/"]
RUN dotnet restore "SAMS/SAMS.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/SAMS"
RUN dotnet build "SAMS.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "SAMS.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Build runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Install the Cloud SQL Auth Proxy
RUN apt-get update && apt-get install -y wget && \
    wget https://dl.google.com/cloudsql/cloud_sql_proxy.linux.amd64 -O /usr/local/bin/cloud_sql_proxy && \
    chmod +x /usr/local/bin/cloud_sql_proxy

# Make sure the app runs on the port Cloud Run expects
ENV PORT 8080
ENV ASPNETCORE_URLS=http://+:${PORT}

# Run the application
ENTRYPOINT ["dotnet", "SAMS.dll"]