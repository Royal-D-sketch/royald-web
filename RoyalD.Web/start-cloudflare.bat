@echo off
chcp 65001 > nul
echo ==============================================================================
echo  🚀 Starting Royal-D Accounts Receivable System on Cloudflare Edge Hosting
echo ==============================================================================

echo [1/2] Starting ASP.NET Core Web Application...
start /B dotnet run --urls "http://localhost:5050"

timeout /t 3 /nobreak > nul

echo [2/2] Connecting to Cloudflare Global Edge Network...
.\cloudflared.exe tunnel --url http://localhost:5050

pause
