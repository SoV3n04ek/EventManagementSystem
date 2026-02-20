# --- Build Stage ---
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Update paths to include the 'backend' prefix
COPY ["backend/EventManagement.Api/EventManagement.Api.csproj", "EventManagement.Api/"]
COPY ["backend/EventManagement.Application/EventManagement.Application.csproj", "EventManagement.Application/"]
COPY ["backend/EventManagement.Domain/EventManagement.Domain.csproj", "EventManagement.Domain/"]
COPY ["backend/EventManagement.Infrastructure/EventManagement.Infrastructure.csproj", "EventManagement.Infrastructure/"]

RUN dotnet restore "EventManagement.Api/EventManagement.Api.csproj"

# Copy everything from the backend folder
COPY backend/ . 
WORKDIR "/src/EventManagement.Api"

# Build and Publish
RUN dotnet publish "EventManagement.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# --- Runtime Stage ---
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS final
WORKDIR /app

# 1. Create the directory for keys
# 2. Change ownership to the 'app' user (UID 1654 in .NET images)
RUN mkdir -p /home/app/.aspnet/DataProtection-Keys && \
    chown -R 1654:1654 /home/app/.aspnet/

# Now switch to the non-root user (for security reasons)
USER $APP_UID

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "EventManagement.Api.dll"]