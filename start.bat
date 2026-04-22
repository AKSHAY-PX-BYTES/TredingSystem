@echo off
title AI Trading System Launcher
echo ============================================
echo    AI Trading Strategy Predictor
echo    Starting Backend API + Blazor Frontend
echo ============================================
echo.

echo [1/2] Starting Backend API (https://localhost:5001)...
start "TradingSystem API" cmd /k "cd /d %~dp0src\TradingSystem.Api && dotnet run"

echo [2/2] Starting Frontend UI (https://localhost:5002)...
timeout /t 5 /nobreak >nul
start "TradingSystem Web" cmd /k "cd /d %~dp0src\TradingSystem.Web && dotnet run"

echo.
echo Both services are starting...
echo Waiting for build to complete before opening browsers...
echo.

:: Wait for API to be ready (check every 3 seconds, up to 60 seconds)
echo Waiting for API (https://localhost:5001)...
set /a attempts=0
:WAIT_API
timeout /t 3 /nobreak >nul
set /a attempts+=1
curl -sk -o nul -w "%%{http_code}" https://localhost:5001/swagger/index.html >nul 2>&1
if %errorlevel% equ 0 (
    echo    API is ready!
    goto API_READY
)
if %attempts% geq 20 (
    echo    Timeout waiting for API, opening browsers anyway...
    goto API_READY
)
goto WAIT_API

:API_READY
:: Wait a bit more for Frontend to be ready
echo Waiting for Frontend (https://localhost:5002)...
set /a attempts=0
:WAIT_WEB
timeout /t 3 /nobreak >nul
set /a attempts+=1
curl -sk -o nul -w "%%{http_code}" https://localhost:5002 >nul 2>&1
if %errorlevel% equ 0 (
    echo    Frontend is ready!
    goto WEB_READY
)
if %attempts% geq 20 (
    echo    Timeout waiting for Frontend, opening browsers anyway...
    goto WEB_READY
)
goto WAIT_WEB

:WEB_READY
echo.
echo Opening in default browser...
start "" "https://localhost:5001/swagger"
timeout /t 2 /nobreak >nul
start "" "https://localhost:5002"

echo.
echo ============================================
echo    Both services are running!
echo.
echo    API + Swagger : https://localhost:5001/swagger
echo    Frontend UI   : https://localhost:5002
echo ============================================
echo.
echo Press any key to exit this launcher window...
pause >nul
