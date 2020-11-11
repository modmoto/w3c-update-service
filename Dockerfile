FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build-env

WORKDIR /app
COPY ./w3c-update-service.sln ./

COPY ./w3c-update-service/w3c-update-service.csproj ./w3c-update-service/w3c-update-service.csproj
RUN dotnet restore ./w3c-update-service/w3c-update-service.csproj

COPY ./w3c-update-service ./w3c-update-service
RUN dotnet build ./w3c-update-service/w3c-update-service.csproj -c Release

RUN dotnet publish "./w3c-update-service/w3c-update-service.csproj" -c Release -o "../../app/out"

FROM mcr.microsoft.com/dotnet/aspnet:5.0
WORKDIR /app
COPY --from=build-env /app/out .

ENV ASPNETCORE_URLS http://*:80
EXPOSE 80

ENTRYPOINT dotnet w3c-update-service.dll