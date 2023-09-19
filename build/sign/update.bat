del signclient.exe
rmdir %~dp0\.store /s /q
dotnet tool install SignClient --tool-path .