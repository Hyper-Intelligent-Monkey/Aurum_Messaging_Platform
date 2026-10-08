FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /app

# Copy solution, props, and project files for caching dependency restores
COPY MessagingServer.slnx ./
COPY Directory.Build.props ./
COPY Directory.Packages.props ./
COPY src/Domain/Domain.csproj src/Domain/
COPY src/Application/Application.csproj src/Application/
COPY src/Infrastructure/Infrastructure.csproj src/Infrastructure/
COPY src/Api/Api.csproj src/Api/

RUN dotnet restore src/Api/Api.csproj

# Copy all source files and publish
COPY . ./
RUN dotnet publish src/Api/Api.csproj -c Release -o /app/publish --no-restore

# Stage 2: Minimal runtime image
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

# Create uploads folder inside the container
RUN mkdir -p /app/uploads

COPY --from=build /app/publish ./

# Expose HTTP port
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Api.dll"]
