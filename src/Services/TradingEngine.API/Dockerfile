FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/Services/TradingEngine.API/TradingEngine.API.csproj", "src/Services/TradingEngine.API/"]
COPY ["src/Shared/Common/Common.csproj", "src/Shared/Common/"]
COPY ["src/Shared/EventContracts/EventContracts.csproj", "src/Shared/EventContracts/"]
COPY ["src/Shared/Infrastructure/Infrastructure.csproj", "src/Shared/Infrastructure/"]
RUN dotnet restore "src/Services/TradingEngine.API/TradingEngine.API.csproj"
COPY . .
WORKDIR "/src/src/Services/TradingEngine.API"
RUN dotnet build "TradingEngine.API.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TradingEngine.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TradingEngine.API.dll"]
