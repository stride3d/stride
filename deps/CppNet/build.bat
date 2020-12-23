@echo off
setlocal
set CPPNET=%~dp0..\..\externals\CppNet
call "%ProgramFiles(x86)%\Microsoft Visual Studio\2019\Community\VC\Auxiliary\Build\vcvarsall.bat" x86

pushd %CPPNET%

rem Build Non-portable version
msbuild /nologo /p:Configuration=Release;Platform="Any CPU" CppNet.sln
if ERRORLEVEL 1 echo "Cannot build CppNet" && pause

popd

mkdir netstandard1.3
xcopy /Y /Q %CPPNET%\Bin\Release\CppNet.dll netstandard1.3 > nul
if ERRORLEVEL 1  echo "Cannot copy CppNet.dll" && pause
xcopy /Y /Q %CPPNET%\Bin\Release\CppNet.pdb netstandard1.3 > nul
if ERRORLEVEL 1  echo "Cannot copy CppNet.pdb" && pause

echo CppNet build completed successfully
