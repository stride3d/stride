CALL ..\find_git.cmd
IF NOT ERRORLEVEL 0 (
  ECHO "Could not find git.exe"
  EXIT /B %ERRORLEVEL%
) 
%GIT_CMD% clone https://git.xiph.org/opus.git -b v1.1.3 ..\..\externals\Celt
if NOT ERRORLEVEL 0 pause
