FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY services/Orders.Service/Orders.Service.csproj services/Orders.Service/
RUN dotnet restore services/Orders.Service/Orders.Service.csproj
COPY services/Orders.Service/ services/Orders.Service/
RUN dotnet publish services/Orders.Service/Orders.Service.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Orders.Service.dll"]