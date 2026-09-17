# Utiliser l'image SDK .NET officielle pour builder
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copier le projet et restaurer les dépendances
COPY ["EasySave.LogServer.csproj", "./"]
RUN dotnet restore "EasySave.LogServer.csproj"

# Copier le reste du code et compiler
COPY . .
RUN dotnet publish "EasySave.LogServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Créer l'image finale légère avec uniquement le runtime
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Le serveur écoute sur le port 8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "EasySave.LogServer.dll"]
