@echo off

if "%1" == "" (
	echo Missing Debug or Release argument
	EXIT /B 1
)

pushd ..\..\externals\SDL2-CS

REM SDL2-CS
msbuild /p:Configuration="%1" SDL2-CS.Core.csproj /restore
if %ERRORLEVEL% neq 0 (
	echo Error during compilation
	popd
	EXIT /B %ERRORLEVEL%
)

popd

rem Copying assemblies
copy ..\..\externals\SDL2-CS\bin\%1\netstandard2.0\*.* .
