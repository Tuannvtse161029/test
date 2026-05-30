# Multi-stage build for ASP.NET Core 9.0 Web API
# 1. Build Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files and restore dependencies
COPY ["ScopusSwaggerTester.csproj", "./"]
RUN dotnet restore

# Copy the rest of the files (including wwwroot static files) and compile
COPY . .
RUN dotnet publish -c Release -o /app/publish

# 2. Runtime Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Expose default port (Render will override this with PORT environment variable)
EXPOSE 5123
ENV PORT=5123

# Run the compiled DLL
ENTRYPOINT ["dotnet", "ScopusSwaggerTester.dll"]
