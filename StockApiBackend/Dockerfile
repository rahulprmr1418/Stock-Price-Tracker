# Stage 1: Build the application
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["StockApiBackend/StockApiBackend.csproj", "StockApiBackend/"]
RUN dotnet restore "StockApiBackend/StockApiBackend.csproj"
COPY . .
WORKDIR "/src/StockApiBackend"
RUN dotnet build "StockApiBackend.csproj" -c Release -o /app/build

# Stage 2: Publish the application
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080

COPY --from=build /app/build .
ENTRYPOINT ["dotnet", "StockApiBackend.dll"]