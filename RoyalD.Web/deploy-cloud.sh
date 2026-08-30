#!/bin/bash
set -e

echo "=========================================================="
echo "🚀 Deploying Royal-D Accounts Receivable System to Cloud"
echo "=========================================================="

# Check Docker & Docker Compose
if ! command -v docker &> /dev/null; then
    echo "❌ Docker not found. Please install Docker first: https://docs.docker.com/engine/install/"
    exit 1
fi

echo "📦 Building and Launching Containers (Web + PostgreSQL)..."
docker compose down || true
docker compose build --no-cache
docker compose up -d

echo ""
echo "=========================================================="
echo "✅ Cloud Production Deployment Completed Successfully!"
echo "🌐 Web Application is running on:"
echo "   -> http://YOUR_SERVER_IP:5050"
echo "   -> http://YOUR_SERVER_IP"
echo ""
echo "🔑 Initial Administrator Credentials:"
echo "   Username: admin"
echo "   Password: admin1234"
echo "=========================================================="
