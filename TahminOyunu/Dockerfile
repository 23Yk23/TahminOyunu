FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["TahminOyunu.csproj", "."]
RUN dotnet restore "TahminOyunu.csproj"
COPY . .
RUN dotnet build "TahminOyunu.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "TahminOyunu.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "TahminOyunu.dll"]
