FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY services/Payments.Service/Payments.Service.csproj services/Payments.Service/
RUN dotnet restore services/Payments.Service/Payments.Service.csproj
COPY services/Payments.Service/ services/Payments.Service/
RUN dotnet publish services/Payments.Service/Payments.Service.csproj -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Payments.Service.dll"]