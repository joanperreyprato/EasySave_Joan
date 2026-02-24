# Étape 1: Build de l'application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copier le fichier projet et restaurer les dépendances
COPY ["EasySave.LogServer/EasySave.LogServer.csproj", "EasySave.LogServer/"]
RUN dotnet restore "EasySave.LogServer/EasySave.LogServer.csproj"

# Copier tous les fichiers et builder
COPY . .
WORKDIR "/src/EasySave.LogServer"
RUN dotnet build "EasySave.LogServer.csproj" -c Release -o /app/build

# Étape 2: Publish
FROM build AS publish
RUN dotnet publish "EasySave.LogServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Étape 3: Runtime final (image plus légère)
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app

# Créer le répertoire pour les logs
RUN mkdir -p /app/logs

# Copier les fichiers publiés
COPY --from=publish /app/publish .

# Exposer le port du serveur (configurable via ENV)
EXPOSE 5555

# Variables d'environnement par défaut
ENV LOG_SERVER_PORT=5555
ENV LOG_SERVER_DIRECTORY=/app/logs

# Point d'entrée
ENTRYPOINT ["dotnet", "EasySave.LogServer.dll"]
