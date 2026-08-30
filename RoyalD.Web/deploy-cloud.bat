@echo off
chcp 65001 > nul
echo ==========================================================
echo 🚀 Deploying Royal-D Accounts Receivable System to Cloud
echo ==========================================================

docker compose down
docker compose build --no-cache
docker compose up -d

echo.
echo ==========================================================
echo ✅ Cloud Production Deployment Completed Successfully!
echo 🌐 Web Application is running on:
echo    -^> http://localhost:5050
echo    -^> http://localhost:80
echo.
echo 🔑 Initial Administrator Credentials:
echo    Username: admin
echo    Password: admin1234
echo ==========================================================
pause
