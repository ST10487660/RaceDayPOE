# ---- build stage ----
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restore first (separate layer so dependencies are cached until the project file changes)
COPY src/RaceDay.Web/RaceDay.Web.csproj src/RaceDay.Web/
RUN dotnet restore src/RaceDay.Web/RaceDay.Web.csproj

# Copy the rest of the source and publish
COPY src/RaceDay.Web/ src/RaceDay.Web/
RUN dotnet publish src/RaceDay.Web/RaceDay.Web.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---- runtime stage (small image, no SDK) ----
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Run as the non-root user provided by the .NET images
USER app

ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    Api__BaseUrl=http://host.docker.internal:5080 \
    Api__PublicBaseUrl=http://localhost:5080

EXPOSE 8080
ENTRYPOINT ["dotnet", "RaceDay.Web.dll"]
