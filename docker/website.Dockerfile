FROM node:22-alpine AS client-build
WORKDIR /src/src/CloudFileManager.Presentation/CloudFileManager.Presentation.Website/ClientApp

ARG VITE_API_BASE_URL=http://localhost:5181
ENV VITE_API_BASE_URL=${VITE_API_BASE_URL}
ARG VITE_API_KEY=dev-local-api-key
ENV VITE_API_KEY=${VITE_API_KEY}

COPY ["src/CloudFileManager.Presentation/CloudFileManager.Presentation.Website/ClientApp/package.json", "./"]
COPY ["src/CloudFileManager.Presentation/CloudFileManager.Presentation.Website/ClientApp/package-lock.json", "./"]
RUN npm ci --no-audit --no-fund

COPY ["src/CloudFileManager.Presentation/CloudFileManager.Presentation.Website/ClientApp/", "./"]
RUN npm run build

FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src
COPY ["TrialProject.sln", "."]
COPY ["src/CloudFileManager.Contracts/CloudFileManager.Contracts.csproj", "src/CloudFileManager.Contracts/"]
COPY ["src/CloudFileManager.Shared/CloudFileManager.Shared.csproj", "src/CloudFileManager.Shared/"]
COPY ["src/CloudFileManager.Presentation/CloudFileManager.Presentation.Website/CloudFileManager.Presentation.Website.csproj", "src/CloudFileManager.Presentation/CloudFileManager.Presentation.Website/"]
RUN dotnet restore "src/CloudFileManager.Presentation/CloudFileManager.Presentation.Website/CloudFileManager.Presentation.Website.csproj"

COPY . .
COPY --from=client-build /src/src/CloudFileManager.Presentation/CloudFileManager.Presentation.Website/wwwroot ./src/CloudFileManager.Presentation/CloudFileManager.Presentation.Website/wwwroot
RUN dotnet publish "src/CloudFileManager.Presentation/CloudFileManager.Presentation.Website/CloudFileManager.Presentation.Website.csproj" -c Release -o /app/publish /p:UseAppHost=false /p:SkipClientBuild=true --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080 \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080

ENTRYPOINT ["dotnet", "CloudFileManager.Presentation.Website.dll"]
