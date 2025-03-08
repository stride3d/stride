@echo OFF
setlocal
set HOME=%USERPROFILE%
CALL ..\find_git.cmd
IF NOT ERRORLEVEL 0 (
  ECHO "Could not find git.exe"
  EXIT /B %ERRORLEVEL%
) 
%GIT_CMD% clone https://github.com/stride3d/BulletSharpPInvoke ../../externals/BulletSharpPInvoke
pushd ..\..\externals\BulletSharpPInvoke
%GIT_CMD% checkout 0de0f3cd564173474c58d57aad31c8aad82cb99d
popd
if NOT ERRORLEVEL 0 pause
