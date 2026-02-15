FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY ["TrialProject.sln", "."]
COPY ["src/CloudFileManager.Domain/CloudFileManager.Domain.csproj", "src/CloudFileManager.Domain/"]
COPY ["src/CloudFileManager.Shared/CloudFileManager.Shared.csproj", "src/CloudFileManager.Shared/"]
COPY ["src/CloudFileManager.Contracts/CloudFileManager.Contracts.csproj", "src/CloudFileManager.Contracts/"]
COPY ["src/CloudFileManager.Application/CloudFileManager.Application.csproj", "src/CloudFileManager.Application/"]
COPY ["src/CloudFileManager.Infrastructure/CloudFileManager.Infrastructure.csproj", "src/CloudFileManager.Infrastructure/"]
COPY ["src/CloudFileManager.Presentation/CloudFileManager.Presentation.Console/CloudFileManager.Presentation.Console.csproj", "src/CloudFileManager.Presentation/CloudFileManager.Presentation.Console/"]

RUN dotnet restore "src/CloudFileManager.Presentation/CloudFileManager.Presentation.Console/CloudFileManager.Presentation.Console.csproj"

COPY . .
RUN dotnet publish "src/CloudFileManager.Presentation/CloudFileManager.Presentation.Console/CloudFileManager.Presentation.Console.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0-preview AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENTRYPOINT ["dotnet", "CloudFileManager.Presentation.Console.dll"]
