# Build from the repository root: docker build -f src/Arte.Sport.Api/Dockerfile .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/Arte.Sport.Core/Arte.Sport.Core.csproj src/Arte.Sport.Core/
COPY src/Arte.Sport.Api/Arte.Sport.Api.csproj src/Arte.Sport.Api/
RUN dotnet restore src/Arte.Sport.Api/Arte.Sport.Api.csproj
COPY src/ src/
RUN dotnet publish src/Arte.Sport.Api/Arte.Sport.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_gcServer=0
USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "Arte.Sport.Api.dll"]
