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

# Make sure the app runs on the port Cloud Run expects
ENV PORT 8080
ENV ASPNETCORE_URLS=http://+:${PORT}

# Run the application
ENTRYPOINT ["dotnet", "SAMS.dll"]