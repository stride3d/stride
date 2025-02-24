@echo OFF
setlocal
set HOME=%USERPROFILE%
CALL ..\find_git.cmd
IF NOT ERRORLEVEL 0 (
  ECHO "Could not find git.exe"
  EXIT /B %ERRORLEVEL%
) 
%GIT_CMD% clone https://github.com/recastnavigation/recastnavigation.git %~dp0..\..\externals\recast
pushd %~dp0..\..\externals\recast
%GIT_CMD% checkout d11c1bdbac8dc0cc0e96613d515f9c1ae3457d58
popd
if NOT ERRORLEVEL 0 pause