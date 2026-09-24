FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["SalesPerf.Backend.csproj", "./"]
RUN dotnet restore "./SalesPerf.Backend.csproj"
COPY . .
RUN dotnet publish "SalesPerf.Backend.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "SalesPerf.Backend.dll"]
