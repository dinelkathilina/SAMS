# Use the official .NET SDK image to build the project
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

# Use a non-root user for better security
USER app

# Expose port 5000 instead of 8080/8081
EXPOSE 5000

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Set the entry point
ENTRYPOINT ["dotnet", "SAMS.dll"]