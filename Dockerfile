# ==============================================================================
#  Royal-D Accounts Receivable Web Application - Production Dockerfile
#  Multi‑stage build: Next.js frontend + .NET 6 API
# ==============================================================================

# ---------- Stage 1: Build Next.js ----------
FROM node:20-alpine AS frontend
WORKDIR /frontend
COPY royald-frontend/package*.json ./
RUN npm ci
COPY royald-frontend/ ./
# Build static export (out folder)
RUN npm run build && npm run export

# ---------- Stage 2: Build .NET API ----------
FROM mcr.microsoft.com/dotnet/sdk:6.0 AS backend
WORKDIR /src
COPY RoyalD.Web/*.csproj ./
RUN dotnet restore
COPY RoyalD.Web/ ./
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

# ---------- Stage 3: Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:6.0-alpine AS runtime
WORKDIR /app
# Copy API
COPY --from=backend /app/publish .
# Copy Frontend static files into wwwroot (served by ASP.NET Core)
COPY --from=frontend /frontend/out ./wwwroot
# Create uploads directory with write permissions
RUN mkdir -p /app/wwwroot/uploads && chmod 777 /app/wwwroot/uploads

# Expose the port Render will use (default 8080)
EXPOSE 8080
# Set ASP.NET to listen on 0.0.0.0:8080
ENV ASPNETCORE_URLS=http://0.0.0.0:8080
ENTRYPOINT ["dotnet", "RoyalD.Web.dll"]
