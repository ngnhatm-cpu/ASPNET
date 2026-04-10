# Cơ bản: Build Stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy file csproj và restore dependencies
COPY ["ASPNET/ASPNET.csproj", "ASPNET/"]
RUN dotnet restore "ASPNET/ASPNET.csproj"

# Copy toàn bộ code và build
COPY . .
WORKDIR "/src/ASPNET"
RUN dotnet build "ASPNET.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "ASPNET.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
COPY --from=publish /app/publish .

# Biến môi trường để chạy trên cổng 8080 (Render mặc định dùng cổng này)
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "ASPNET.dll"]
