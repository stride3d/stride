pushd ..\..\externals\bullet2-sharp-mobile\src\Windows
REM Compile bullet
call build-bullet-windows.bat
if %ERRORLEVEL% neq 0 GOTO :error_popd

REM Compile libbulletc wrapper
call build-libbulletc-windows.bat
if %ERRORLEVEL% neq 0 GOTO :error_popd
popd

REM Create folders
mkdir Windows\x86
mkdir Windows\x64
mkdir WindowsStore\x86
mkdir WindowsStore\x64
mkdir WindowsStore\ARM
mkdir WindowsPhone\x86
mkdir WindowsPhone\ARM
mkdir UWP\x86
mkdir UWP\x64
mkdir UWP\ARM

copy ..\..\externals\bullet2-sharp-mobile\src\build\bulletc\Windows\x86\Release\*.dll Windows\x86
copy ..\..\externals\bullet2-sharp-mobile\src\build\bulletc\Windows\x64\Release\*.dll Windows\x64
copy ..\..\externals\bullet2-sharp-mobile\src\build\bulletc\WindowsStore\x86\Release\*.dll WindowsStore\x86
copy ..\..\externals\bullet2-sharp-mobile\src\build\bulletc\WindowsStore\x64\Release\*.dll WindowsStore\x64
copy ..\..\externals\bullet2-sharp-mobile\src\build\bulletc\WindowsStore\ARM\Release\*.dll WindowsStore\ARM
copy ..\..\externals\bullet2-sharp-mobile\src\build\bulletc\WindowsPhone\x86\Release\*.dll WindowsPhone\x86
copy ..\..\externals\bullet2-sharp-mobile\src\build\bulletc\WindowsPhone\ARM\Release\*.dll WindowsPhone\ARM
copy ..\..\externals\bullet2-sharp-mobile\src\build\bulletc\Windows10\x86\Release\*.dll UWP\x86
copy ..\..\externals\bullet2-sharp-mobile\src\build\bulletc\Windows10\x64\Release\*.dll UWP\x64
copy ..\..\externals\bullet2-sharp-mobile\src\build\bulletc\Windows10\ARM\Release\*.dll UWP\ARM

GOTO :end
:error_popd
popd
echo Error during compilation
EXIT /B %ERRORLEVEL%
pause
:end
