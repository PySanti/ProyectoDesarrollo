FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY src/Umbral.TeamService.Domain/Umbral.TeamService.Domain.csproj src/Umbral.TeamService.Domain/
COPY src/Umbral.TeamService.Application/Umbral.TeamService.Application.csproj src/Umbral.TeamService.Application/
COPY src/Umbral.TeamService.Infrastructure/Umbral.TeamService.Infrastructure.csproj src/Umbral.TeamService.Infrastructure/
COPY src/Umbral.TeamService.Api/Umbral.TeamService.Api.csproj src/Umbral.TeamService.Api/

RUN dotnet restore src/Umbral.TeamService.Api/Umbral.TeamService.Api.csproj

COPY src/ src/

RUN dotnet publish src/Umbral.TeamService.Api/Umbral.TeamService.Api.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

EXPOSE 8080

ENTRYPOINT ["dotnet", "Umbral.TeamService.Api.dll"]
