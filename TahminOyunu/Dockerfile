FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Bağımlı projeleri ve ana projeyi kopyala
COPY ["BusinessLayer/BusinessLayer.csproj", "BusinessLayer/"]
COPY ["DataAccessLayer/DataAccessLayer.csproj", "DataAccessLayer/"]
COPY ["EntityLayer/EntityLayer.csproj", "EntityLayer/"]
COPY ["TahminOyunu/TahminOyunu.csproj", "TahminOyunu/"]

RUN dotnet restore "TahminOyunu/TahminOyunu.csproj"

# Tüm solution içeriğini kopyala
COPY . .

WORKDIR "/src/TahminOyunu"
RUN dotnet build "TahminOyunu.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TahminOyunu.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TahminOyunu.dll"]
