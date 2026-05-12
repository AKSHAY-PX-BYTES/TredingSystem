@echo off
REM Reads FRONTEND value from frontend.config and copies the correct netlify.toml

for /f "tokens=2 delims==" %%a in ('findstr "FRONTEND=" frontend.config') do set FRONTEND=%%a

echo Frontend selected: %FRONTEND%

if "%FRONTEND%"=="angular" (
    copy /Y src\TradingSystem.Angular\netlify.toml netlify.toml
    echo Activated Angular configuration
) else if "%FRONTEND%"=="blazor" (
    copy /Y src\TradingSystem.Web\netlify.toml netlify.toml
    echo Activated Blazor configuration
) else (
    echo ERROR: Invalid FRONTEND value. Use 'angular' or 'blazor'
    exit /b 1
)

echo Done. Now commit and push netlify.toml to deploy.
