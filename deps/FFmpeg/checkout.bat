CALL ..\find_git.cmd
IF NOT ERRORLEVEL 0 (
  ECHO "Could not find git.exe"
  EXIT /B %ERRORLEVEL%
) 
%GIT_CMD% clone https://git.ffmpeg.org/ffmpeg.git ../../externals/ffmpeg
rem checkout n3.3.3 tag
IF NOT ERRORLEVEL 0 PAUSE