del signclient.exe
rmdir %~dp0\.store /s /q
dotnet tool install sign --tool-path . --version 0.9.0-beta.23127.3