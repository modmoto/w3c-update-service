FROM mcr.microsoft.com/dotnet/core/sdk:3.1 AS build-env

WORKDIR /app
COPY ./w3c-update-service.sln.sln ./

COPY ./w3c-update-service/w3c-update-service.csproj.csproj ./w3c-update-service/w3c-update-service.csproj.csproj
RUN dotnet restore ./w3c-update-service/w3c-update-service.csproj.csproj

COPY ./w3c-update-service ./w3c-update-service
RUN dotnet build ./w3c-update-service/w3c-update-service.csproj.csproj -c Release

RUN dotnet publish "./w3c-update-service/w3c-update-service.csproj.csproj" -c Release -o "../../app/out"

FROM mcr.microsoft.com/dotnet/core/aspnet:3.1
WORKDIR /app
COPY --from=build-env /app/out .

ENV ASPNETCORE_URLS http://*:80
EXPOSE 80

ENTRYPOINT dotnet w3c-update-service.dll