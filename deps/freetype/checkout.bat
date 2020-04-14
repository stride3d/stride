CALL ..\find_git.cmd
IF NOT ERRORLEVEL 0 (
  ECHO "Could not find git.exe"
  EXIT /B %ERRORLEVEL%
) 
%GIT_CMD% clone https://github.com/stride3d/freetype.git -b 2.6.3 ../../externals/freetype
if NOT ERRORLEVEL 0 pause
pushd ..\..\externals\freetype
%GIT_CMD% remote add upstream http://git.sv.nongnu.org/r/freetype/freetype2.git
%GIT_CMD% fetch --all
popd
if NOT ERRORLEVEL 0 pause
