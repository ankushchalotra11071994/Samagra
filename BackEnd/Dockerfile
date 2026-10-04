FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Samagra.API/Samagra.API.csproj Samagra.API/
COPY Samagra.Application/Samagra.Application.csproj Samagra.Application/
COPY Samagra.Domain/Samagra.Domain.csproj Samagra.Domain/
COPY Samagra.Infrastructure/Samagra.Infrastructure.csproj Samagra.Infrastructure/
COPY Samagra.AI/Samagra.AI.csproj Samagra.AI/
RUN dotnet restore Samagra.API/Samagra.API.csproj

COPY . .
RUN dotnet publish Samagra.API/Samagra.API.csproj -c Release -o /app/publish 

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Samagra.API.dll"]