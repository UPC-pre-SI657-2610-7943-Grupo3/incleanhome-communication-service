FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY src/InCleanHome.CommunicationService/InCleanHome.CommunicationService.csproj src/InCleanHome.CommunicationService/
RUN dotnet restore "src/InCleanHome.CommunicationService/InCleanHome.CommunicationService.csproj"
COPY . .
RUN dotnet publish "src/InCleanHome.CommunicationService/InCleanHome.CommunicationService.csproj" \
    -c Release -o /app/publish --no-restore /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
RUN apt-get update && apt-get install -y --no-install-recommends wget && rm -rf /var/lib/apt/lists/*
RUN useradd -m -u 10001 appuser
COPY --from=build --chown=appuser:appuser /app/publish .
# Entrypoint script: materializa el JSON de Firebase (env var de Key Vault) como archivo
COPY --chown=appuser:appuser entrypoint.sh /app/entrypoint.sh
RUN sed -i 's/\r$//' /app/entrypoint.sh && chmod +x /app/entrypoint.sh
USER appuser
EXPOSE 5005
ENV ASPNETCORE_URLS=http://+:5005
ENTRYPOINT ["/app/entrypoint.sh"]