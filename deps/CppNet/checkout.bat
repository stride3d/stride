@echo OFF
setlocal
set HOME=%USERPROFILE%
CALL ..\find_git.cmd
IF NOT ERRORLEVEL 0 (
  ECHO "Could not find git.exe"
  EXIT /B %ERRORLEVEL%
) 
%GIT_CMD% clone --recursive git@github.com:stride3d/CppNet.git -b master ../../externals/CppNet
if ERRORLEVEL 1 echo "Could not checkout CppNet" && pause
