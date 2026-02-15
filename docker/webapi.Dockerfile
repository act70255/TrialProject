FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY ["TrialProject.sln", "."]
COPY ["src/CloudFileManager.Domain/CloudFileManager.Domain.csproj", "src/CloudFileManager.Domain/"]
COPY ["src/CloudFileManager.Shared/CloudFileManager.Shared.csproj", "src/CloudFileManager.Shared/"]
COPY ["src/CloudFileManager.Contracts/CloudFileManager.Contracts.csproj", "src/CloudFileManager.Contracts/"]
COPY ["src/CloudFileManager.Application/CloudFileManager.Application.csproj", "src/CloudFileManager.Application/"]
COPY ["src/CloudFileManager.Infrastructure/CloudFileManager.Infrastructure.csproj", "src/CloudFileManager.Infrastructure/"]
COPY ["src/CloudFileManager.Presentation/CloudFileManager.Presentation.WebApi/CloudFileManager.Presentation.WebApi.csproj", "src/CloudFileManager.Presentation/CloudFileManager.Presentation.WebApi/"]

RUN dotnet restore "src/CloudFileManager.Presentation/CloudFileManager.Presentation.WebApi/CloudFileManager.Presentation.WebApi.csproj"

COPY . .
RUN dotnet publish "src/CloudFileManager.Presentation/CloudFileManager.Presentation.WebApi/CloudFileManager.Presentation.WebApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CloudFileManager.Presentation.WebApi.dll"]
