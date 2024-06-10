# Base stage for running the application
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage for backend
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["backend/MemeBE/MemeBE.csproj", "backend/MemeBE/"]
RUN dotnet restore "backend/MemeBE/MemeBE.csproj"
COPY . .
WORKDIR "/src/backend/MemeBE"
RUN dotnet build "MemeBE.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish stage for backend
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "MemeBE.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Build stage for frontend
FROM node:18 AS frontend-build
WORKDIR /src/client
COPY client/package*.json ./
RUN npm install
COPY client/ ./
RUN npm run build

# Final stage
FROM nginx:alpine AS final
WORKDIR /usr/share/nginx/html

# Copy static files
COPY --from=frontend-build /src/client/build ./

# Copy backend files
WORKDIR /app
COPY --from=publish /app/publish .

# Nginx configuration
COPY nginx.conf /etc/nginx/nginx.conf

# Run the backend
CMD ["dotnet", "MemeBE.dll"]
