@echo OFF
setlocal
set HOME=%USERPROFILE%
CALL ..\find_git.cmd
IF NOT ERRORLEVEL 0 (
  ECHO "Could not find git.exe"
  EXIT /B %ERRORLEVEL%
) 
%GIT_CMD% clone git@github.com:stride3d/opentk ../../externals/opentk -b develop
if NOT ERRORLEVEL 0 pause
pushd ..\..\externals\opentk
%GIT_CMD% remote add upstream git@github.com:opentk/opentk.git
%GIT_CMD% fetch --all
popd
if NOT ERRORLEVEL 0 pause