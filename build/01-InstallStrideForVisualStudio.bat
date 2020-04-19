@echo off
setlocal
rmdir /S /Q "%LOCALAPPDATA%\temp\Stride"
REM -------------------------------------------------------------------
REM Global config
REM -------------------------------------------------------------------
set STRIDE_OUTPUT_DIR=%~dp0\..\Bin
set STRIDE_VSIX=%STRIDE_OUTPUT_DIR%\VSIX\Stride.vsix
REM -------------------------------------------------------------------
REM Build Stride
REM -------------------------------------------------------------------
IF EXIST "%STRIDE_VSIX%" GOTO :vsixok
echo Error, unable to find Stride VSIX [%STRIDE_VSIX%]
echo Run 00-BuildStride.bat before trying to install the VisualStudio package
pause
exit /b 1
:vsixok
"%STRIDE_VSIX%"
