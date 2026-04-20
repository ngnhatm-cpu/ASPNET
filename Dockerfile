# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution và project file
COPY ["ASPNET.sln", "./"]
COPY ["ASPNET/ASPNET.csproj", "ASPNET/"]

# Restore dependencies
RUN dotnet restore

# Copy toàn bộ code và build
COPY . .
WORKDIR "/src/ASPNET"
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Sử dụng biến PORT mặc định của Render (thường là 10000)
ENV ASPNETCORE_URLS=http://+:10000
EXPOSE 10000

ENTRYPOINT ["dotnet", "ASPNET.dll"]
