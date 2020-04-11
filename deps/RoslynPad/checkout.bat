@echo OFF
setlocal
set HOME=%USERPROFILE%
CALL ..\find_git.cmd
IF NOT ERRORLEVEL 0 (
  ECHO "Could not find git.exe"
  EXIT /B %ERRORLEVEL%
) 
%GIT_CMD% clone --recursive https://github.com/aelij/RoslynPad -b master ../../externals/RoslynPad
pushd ..\..\externals\RoslynPad
%GIT_CMD% checkout 0d576ca5d18d67d447bce5d078f0a568a69a677d
popd
if ERRORLEVEL 1 echo "Could not checkout RoslynPad" && pause
