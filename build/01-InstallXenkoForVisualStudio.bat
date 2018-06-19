@echo off
setlocal
rmdir /S /Q "%LOCALAPPDATA%\temp\Xenko"
REM -------------------------------------------------------------------
REM Global config
REM -------------------------------------------------------------------
set XENKO_OUTPUT_DIR=%~dp0\..\Bin
set XENKO_VSIX=%XENKO_OUTPUT_DIR%\VSIX\Xenko.vsix
REM -------------------------------------------------------------------
REM Build Xenko
REM -------------------------------------------------------------------
IF EXIST "%XENKO_VSIX%" GOTO :vsixok
echo Error, unable to find Xenko VSIX [%XENKO_VSIX%]
echo Run 00-BuildXenko.bat before trying to install the VisualStudio package
pause
exit /b 1
:vsixok
"%XENKO_VSIX%"
