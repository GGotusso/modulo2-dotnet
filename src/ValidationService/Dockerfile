FROM mcr.microsoft.com/dotnet/runtime:8.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["src/ValidationService/ValidationService.csproj", "ValidationService/"]
COPY ["src/Shared/Shared.csproj", "Shared/"]
RUN dotnet restore "ValidationService/ValidationService.csproj"
COPY src/ .
WORKDIR "/src/ValidationService"
RUN dotnet build "ValidationService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "ValidationService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "ValidationService.dll"]
