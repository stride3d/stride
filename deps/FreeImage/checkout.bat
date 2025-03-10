@echo OFF
setlocal
set HOME=%USERPROFILE%
CALL ..\find_git.cmd
IF NOT ERRORLEVEL 0 (
  ECHO "Could not find git.exe"
  EXIT /B %ERRORLEVEL%
) 
%GIT_CMD% clone https://github.com/danoli3/FreeImage.git %~dp0..\..\externals\FreeImage
pushd %~dp0..\..\externals\FreeImage
%GIT_CMD% checkout 3.16.0
%GIT_CMD% apply ..\..\deps\freeimage\build-fix.patch
popd
if NOT ERRORLEVEL 0 pause