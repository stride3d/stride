@echo off
taskkill /F /IM Stride.CompareGold.exe >nul 2>&1
dotnet run --project "%~dp0..\build\tools\Stride.CompareGold"
