# Stage 1: Build & Publish
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["RoyalD.Web.csproj", "./"]
RUN dotnet restore "./RoyalD.Web.csproj"
COPY . .
RUN dotnet publish "RoyalD.Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime Environment
FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS final
WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

COPY --from=build /app/publish .
RUN mkdir -p /app/wwwroot/uploads

EXPOSE 8080 10000

ENTRYPOINT ["dotnet", "RoyalD.Web.dll"]
