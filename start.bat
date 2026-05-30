@echo off
title Scopus Hub & .NET OpenAPI Tester
color 0b

echo ===================================================================
echo     Scopus Hub & .NET OpenAPI Swagger Tester
echo     Local Deployment Server - Powered by ASP.NET Core 9.0
echo ===================================================================
echo.
echo [1/2] Starting local .NET Web API backend...
echo.

:: Launch the browser in a background command thread after waiting 3 seconds
:: to give the .NET compiler and bootstrap server enough time to launch.
start /b cmd /c "timeout /t 3 /nobreak >nul && start http://localhost:5123"

:: Run the ASP.NET Core app using the "http" profile (port 5123)
dotnet run --launch-profile "http"

if %ERRORLEVEL% neq 0 (
    echo.
    echo [ERROR] Failed to start ASP.NET Core service. Please make sure:
    echo   1. No other service is running on port 5123
    echo   2. You have the .NET 9 SDK installed correctly (running 'dotnet --version')
    echo.
    pause
)
