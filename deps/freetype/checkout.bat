CALL ..\find_git.cmd
IF NOT ERRORLEVEL 0 (
  ECHO "Could not find git.exe"
  EXIT /B %ERRORLEVEL%
) 
%GIT_CMD% clone https://github.com/stride3d/freetype.git -b 2.6.3 ../../externals/freetype
if NOT ERRORLEVEL 0 pause
pushd ..\..\externals\freetype
%GIT_CMD% checkout 84631a11ab76240581f621287b260a6936a9014c
popd
if NOT ERRORLEVEL 0 pause

:: Upstream repo can be added via
:: %GIT_CMD% remote add upstream http://git.sv.nongnu.org/r/freetype/freetype2.git
:: %GIT_CMD% fetch --all
