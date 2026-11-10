# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project file first to leverage Docker layer caching for NuGet restore
COPY ["TelemetryApi.csproj", "./"]
RUN dotnet restore "TelemetryApi.csproj"

# Copy remaining source code and publish release build
COPY . .
RUN dotnet publish "TelemetryApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Final Runtime Image
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# Run container as a non-root user for cloud security standards
USER $APP_UID

# Expose standard non-root port used in modern .NET container images
EXPOSE 8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TelemetryApi.dll"]
