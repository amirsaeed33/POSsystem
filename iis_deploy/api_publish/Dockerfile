FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY ["src/SmartPos.Web.Host/SmartPos.Web.Host.csproj", "src/SmartPos.Web.Host/"]
COPY ["src/SmartPos.Web.Core/SmartPos.Web.Core.csproj", "src/SmartPos.Web.Core/"]
COPY ["src/SmartPos.Application/SmartPos.Application.csproj", "src/SmartPos.Application/"]
COPY ["src/SmartPos.Core/SmartPos.Core.csproj", "src/SmartPos.Core/"]
COPY ["src/SmartPos.EntityFrameworkCore/SmartPos.EntityFrameworkCore.csproj", "src/SmartPos.EntityFrameworkCore/"]
WORKDIR "/src/src/SmartPos.Web.Host"
RUN dotnet restore 

WORKDIR /src
COPY ["src/SmartPos.Web.Host", "src/SmartPos.Web.Host"]
COPY ["src/SmartPos.Web.Core", "src/SmartPos.Web.Core"]
COPY ["src/SmartPos.Application", "src/SmartPos.Application"]
COPY ["src/SmartPos.Core", "src/SmartPos.Core"]
COPY ["src/SmartPos.EntityFrameworkCore", "src/SmartPos.EntityFrameworkCore"]
WORKDIR "/src/src/SmartPos.Web.Host"
RUN dotnet publish -c Release -o /publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
EXPOSE 80
WORKDIR /app
COPY --from=build /publish .
ENTRYPOINT ["dotnet", "SmartPos.Web.Host.dll"]
