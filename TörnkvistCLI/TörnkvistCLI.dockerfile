# Använd en officiell .NET SDK-bild som bas
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Ställ in arbetskatalogen i containern
WORKDIR /app

# Kopiera projektfilen och återställ eventuella beroenden
COPY *.csproj ./
RUN dotnet restore

# Kopiera resten av applikationen och bygg den
COPY . ./
RUN dotnet publish -c Release -o out

# Använd en lättvikts-bild för att köra applikationen
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/out .

# Exponera en port (om du kör en webapplikation)
EXPOSE 80

# Starta applikationen
ENTRYPOINT ["dotnet", "TörnkvistCLI.dll"]
