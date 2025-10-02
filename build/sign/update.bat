del sign.exe
rmdir %~dp0\.store /s /q
dotnet tool install sign --tool-path . --version 0.9.1-beta.25278.1