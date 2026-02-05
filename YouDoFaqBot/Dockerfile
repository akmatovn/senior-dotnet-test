# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "YouDoFaqBot.App/YouDoFaqBot.App.csproj"
RUN dotnet publish "YouDoFaqBot.App/YouDoFaqBot.App.csproj" -c Release -o /app/publish --no-restore

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY YouDoFaqBot.App/appsettings.json ./appsettings.json:ro
COPY YouDoFaqBot.App/knowledge_base.json ./knowledge_base.json:ro
ENTRYPOINT ["dotnet", "YouDoFaqBot.App.dll"]
