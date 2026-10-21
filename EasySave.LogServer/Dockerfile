# Build stage: the server reuses the EasyLog library, so the build context is the repository root
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["EasyLog/EasyLog.csproj", "EasyLog/"]
COPY ["EasySave.LogServer/EasySave.LogServer.csproj", "EasySave.LogServer/"]
RUN dotnet restore "EasySave.LogServer/EasySave.LogServer.csproj"

COPY EasyLog/ EasyLog/
COPY EasySave.LogServer/ EasySave.LogServer/
RUN dotnet publish "EasySave.LogServer/EasySave.LogServer.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV EASYSAVE_LOG_DIR=/logs \
    EASYSAVE_LOG_PORT=8080
VOLUME ["/logs"]
EXPOSE 8080

ENTRYPOINT ["dotnet", "EasySave.LogServer.dll"]
