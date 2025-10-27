# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY *.sln ./
COPY EventManagement.Api/*.csproj ./EventManagement.Api/
COPY EventManagement.Application/*.csproj ./EventManagement.Application/
COPY EventManagement.Domain/*.csproj ./EventManagement.Domain/
COPY EventManagement.Infrastructure/*.csproj ./EventManagement.Infrastructure/

# Clear any local NuGet config and restore
RUN dotnet nuget locals all --clear
RUN dotnet restore --verbosity minimal

# Copy everything else
COPY . .

# Build and publish
WORKDIR /src/EventManagement.Api
RUN dotnet publish -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "EventManagement.Api.dll"]