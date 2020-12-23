@echo OFF
setlocal
set HOME=%USERPROFILE%
CALL ..\find_git.cmd
IF NOT ERRORLEVEL 0 (
  ECHO "Could not find git.exe"
  EXIT /B %ERRORLEVEL%
) 
%GIT_CMD% clone "https://github.com/flibitijibibo/SDL2-CS.git" ../../externals/SDL2-CS
pushd ..\..\externals\SDL2-CS
%GIT_CMD% checkout 3e7eaf9d5be29
popd