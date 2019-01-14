CALL ..\find_git.cmd
IF NOT ERRORLEVEL 0 (
  ECHO "Could not find git.exe"
  EXIT /B %ERRORLEVEL%
) 
%GIT_CMD% clone https://github.com/xenko3d/SharpVulkan ../../externals/SharpVulkan
IF NOT ERRORLEVEL 0 PAUSE

