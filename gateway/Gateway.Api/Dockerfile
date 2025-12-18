FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY gateway/Gateway.Api/Gateway.Api.csproj gateway/Gateway.Api/
RUN dotnet restore gateway/Gateway.Api/Gateway.Api.csproj
COPY gateway/Gateway.Api/ gateway/Gateway.Api/
RUN dotnet publish gateway/Gateway.Api/Gateway.Api.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Gateway.Api.dll"]