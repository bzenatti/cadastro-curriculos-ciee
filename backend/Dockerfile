FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
# Restaura os pacotes antes de copiar o código: essa camada fica em cache enquanto o .csproj não mudar.
COPY src/Candidates.Api/Candidates.Api.csproj src/Candidates.Api/
RUN dotnet restore src/Candidates.Api
COPY src/Candidates.Api src/Candidates.Api
RUN dotnet publish src/Candidates.Api -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Candidates.Api.dll"]
