pushd ..\..\externals\BulletSharpPInvoke\src\VHACD_Lib\VHACD

REM Compile vhacd
msbuild VHACD.vcxproj /p:Configuration=Release;Platform=x64
msbuild VHACD.vcxproj /p:Configuration=Release;Platform=Win32
if %ERRORLEVEL% neq 0 GOTO :error_popd
popd

REM Create folders
mkdir x86
mkdir x64

copy ..\..\externals\BulletSharpPInvoke\src\VHACD_Lib\VHACD\Release\*.dll x86
copy ..\..\externals\BulletSharpPInvoke\src\VHACD_Lib\VHACD\x64\Release\*.dll x64

GOTO :end
:error_popd
popd
echo Error during compilation
EXIT /B %ERRORLEVEL%
pause
:end
