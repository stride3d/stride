@echo OFF
setlocal
set HOME=%USERPROFILE%
CALL ..\find_git.cmd
IF NOT ERRORLEVEL 0 (
  ECHO "Could not find git.exe"
  EXIT /B %ERRORLEVEL%
) 
%GIT_CMD% clone git@github.com:stride3d/msdfgen.git ../../externals/msdfgen
if NOT ERRORLEVEL 0 pause

%GIT_CMD% clone git@github.com:leethomason/tinyxml2.git ../../externals/tinyxml2
if NOT ERRORLEVEL 0 pause

%GIT_CMD% clone git@github.com:lvandeve/lodepng.git ../../externals/lodepng
if NOT ERRORLEVEL 0 pause

