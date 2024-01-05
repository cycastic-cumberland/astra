FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Astra.Benchmark/Astra.Benchmark.csproj", "Astra.Benchmark/"]
RUN dotnet restore "Astra.Benchmark/Astra.Benchmark.csproj"
COPY . .
WORKDIR "/src/Astra.Benchmark"
RUN dotnet build "Astra.Benchmark.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Astra.Benchmark.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Astra.Benchmark.dll"]
