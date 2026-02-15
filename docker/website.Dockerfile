FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

COPY ["TrialProject.sln", "."]
COPY ["src/CloudFileManager.Contracts/CloudFileManager.Contracts.csproj", "src/CloudFileManager.Contracts/"]
COPY ["src/CloudFileManager.Presentation/CloudFileManager.Presentation.Website/CloudFileManager.Presentation.Website.csproj", "src/CloudFileManager.Presentation/CloudFileManager.Presentation.Website/"]

RUN dotnet restore "src/CloudFileManager.Presentation/CloudFileManager.Presentation.Website/CloudFileManager.Presentation.Website.csproj"

COPY . .
RUN dotnet publish "src/CloudFileManager.Presentation/CloudFileManager.Presentation.Website/CloudFileManager.Presentation.Website.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "CloudFileManager.Presentation.Website.dll"]
