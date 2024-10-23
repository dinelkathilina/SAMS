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

# Set default port for both environments
ENV PORT=8080
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080
ENTRYPOINT ["dotnet", "SAMS.dll"]