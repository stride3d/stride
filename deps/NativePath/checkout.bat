CALL ..\find_git.cmd
IF NOT ERRORLEVEL 0 (
  ECHO "Could not find git.exe"
  EXIT /B %ERRORLEVEL%
) 
%GIT_CMD% clone https://github.com/stride3d/NativePath ../../externals/NativePath
IF NOT ERRORLEVEL 0 PAUSE
pushd ..\..\externals\NativePath
%GIT_CMD% checkout e615840e57ea80ce30b9e9f29f5fd84ca9b4b253
popd
IF NOT ERRORLEVEL 0 PAUSE

