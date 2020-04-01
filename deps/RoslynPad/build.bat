@echo off
setlocal
set ROSLYNPAD=%~dp0..\..\externals\RoslynPad

pushd %ROSLYNPAD%\src

msbuild /nologo /restore /p:Configuration=Release RoslynPad.sln
if ERRORLEVEL 1 echo "Cannot build RoslynPad" && pause

popd

xcopy /Y /Q %ROSLYNPAD%\src\RoslynPad.Editor.Windows\bin\Release\net462\RoslynPad*.* net462 > nul
if ERRORLEVEL 1  echo "Cannot copy RoslynPad net462 files" && pause

xcopy /Y /Q %ROSLYNPAD%\src\RoslynPad.Editor.Windows\bin\Release\netcoreapp3.1\RoslynPad*.* netcoreapp3.1 > nul
if ERRORLEVEL 1  echo "Cannot copy RoslynPad netcoreapp3.1 files" && pause

echo RoslynPad build completed successfully
