# Use the .NET 8 SDK image for both build and runtime
# We use the full SDK here because the application (Runner.csproj) 
# needs to perform dynamic builds of other projects at runtime.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files and restore dependencies
COPY ["Api_RestAPI_gradingTool/Api_RestAPI_gradingTool.csproj", "Api_RestAPI_gradingTool/"]
COPY ["Application/Application.csproj", "Application/"]
COPY ["Domain/Domain.csproj", "Domain/"]
COPY ["Infrastructure/Infrastructure.csproj", "Infrastructure/"]

RUN dotnet restore "Api_RestAPI_gradingTool/Api_RestAPI_gradingTool.csproj"

# Copy the rest of the source code
COPY . .
WORKDIR "/src/Api_RestAPI_gradingTool"
RUN dotnet build "Api_RestAPI_gradingTool.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Api_RestAPI_gradingTool.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage - using SDK image to support dynamic dotnet build at runtime
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final
WORKDIR /app

# Install migrations tool if needed (optional since we have auto-migrate in Program.cs)
RUN dotnet tool install --global dotnet-ef
ENV PATH="$PATH:/root/.dotnet/tools"

# Copy all projects into a /src directory for the Runner
# This is critical because the Runner.csproj needs to find other project files
WORKDIR /src
COPY . .

# Copy published files
WORKDIR /app
COPY --from=publish /app/publish .

# The published binaries for the API
WORKDIR /app/api-bin
COPY --from=publish /app/publish .

# Set the entry point to the API binary
ENTRYPOINT ["dotnet", "Api_RestAPI_gradingTool.dll"]
