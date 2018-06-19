CALL ..\find_git.cmd
IF NOT ERRORLEVEL 0 (
  ECHO "Could not find git.exe"
  EXIT /B %ERRORLEVEL%
) 
%GIT_CMD% clone https://github.com/xenko3d/SharpFont.git -b sync ../../externals/SharpFont
if NOT ERRORLEVEL 0 pause
