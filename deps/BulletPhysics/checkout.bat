@echo OFF
setlocal
set HOME=%USERPROFILE%
CALL ..\find_git.cmd
IF NOT ERRORLEVEL 0 (
  ECHO "Could not find git.exe"
  EXIT /B %ERRORLEVEL%
) 
%GIT_CMD% clone git@github.com:Eideren/BulletSharpPInvoke.git ../../externals/BulletSharpPInvoke
if NOT ERRORLEVEL 0 pause
