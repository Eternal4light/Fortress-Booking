FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["PartnerService/PartnerService.WebHost.csproj", "PartnerService/"]
COPY ["PartnerService.Core/PartnerService.Core.csproj", "PartnerService.Core/"]
COPY ["PartnerService.DataAccess/PartnerService.DataAccess.csproj", "PartnerService.DataAccess/"]
RUN dotnet restore "PartnerService/PartnerService.WebHost.csproj"
COPY . .
WORKDIR "/src/PartnerService"
RUN dotnet build "PartnerService.WebHost.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PartnerService.WebHost.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PartnerService.WebHost.dll"]
