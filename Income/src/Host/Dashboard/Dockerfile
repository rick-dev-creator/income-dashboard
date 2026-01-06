# See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

# This stage is used when running from VS in fast mode (Default for Debug configuration)
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
USER $APP_UID
WORKDIR /app
EXPOSE 8080
EXPOSE 8081


# This stage is used to build the service project
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Host/Dashboard/Dashboard.csproj", "src/Host/Dashboard/"]
COPY ["src/Modules/Income/Income.Contracts/Income.Contracts.csproj", "src/Modules/Income/Income.Contracts/"]
COPY ["src/Modules/Income/Income.Installer/Income.Installer.csproj", "src/Modules/Income/Income.Installer/"]
COPY ["src/Modules/Income/Income.Domain/Income.Domain.csproj", "src/Modules/Income/Income.Domain/"]
COPY ["src/Shared/Domain.Shared.Kernel/Domain.Shared.Kernel.csproj", "src/Shared/Domain.Shared.Kernel/"]
COPY ["src/Modules/Income/Income.Application/Income.Application.csproj", "src/Modules/Income/Income.Application/"]
COPY ["src/Modules/Income/Income.Infrastructure/Income.Infrastructure.csproj", "src/Modules/Income/Income.Infrastructure/"]
COPY ["src/Modules/Analytics/Analytics.Contracts/Analytics.Contracts.csproj", "src/Modules/Analytics/Analytics.Contracts/"]
COPY ["src/Modules/Analytics/Analytics.Installer/Analytics.Installer.csproj", "src/Modules/Analytics/Analytics.Installer/"]
COPY ["src/Modules/Analytics/Analytics.Domain/Analytics.Domain.csproj", "src/Modules/Analytics/Analytics.Domain/"]
COPY ["src/Modules/Analytics/Analytics.Application/Analytics.Application.csproj", "src/Modules/Analytics/Analytics.Application/"]
COPY ["src/Modules/Analytics/Analytics.Infrastructure/Analytics.Infrastructure.csproj", "src/Modules/Analytics/Analytics.Infrastructure/"]
RUN dotnet restore "./src/Host/Dashboard/Dashboard.csproj"
COPY . .
WORKDIR "/src/src/Host/Dashboard"
RUN dotnet build "./Dashboard.csproj" -c $BUILD_CONFIGURATION -o /app/build

# This stage is used to publish the service project to be copied to the final stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./Dashboard.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# This stage is used in production or when running from VS in regular mode (Default when not using the Debug configuration)
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Dashboard.dll"]