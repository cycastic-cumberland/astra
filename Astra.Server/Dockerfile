FROM mcr.microsoft.com/dotnet/runtime:8.0.0-alpine3.18-amd64 AS base
EXPOSE 8488
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0.100-1-alpine3.18-amd64 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Astra.Server/Astra.Server.csproj", "Astra.Server/"]
RUN dotnet restore "Astra.Server/Astra.Server.csproj"
COPY . .
WORKDIR "/src/Astra.Server"
RUN dotnet build "Astra.Server.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Astra.Server.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Astra.Server.dll"]
