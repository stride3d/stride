@echo off
setlocal
set CPPNET=%~dp0..\..\externals\CppNet
call "%ProgramFiles(x86)%\Microsoft Visual Studio 14.0\vc\vcvarsall.bat" x86

pushd %CPPNET%

rem Build Non-portable version
msbuild /nologo /p:Configuration=Release CppNet.sln
if ERRORLEVEL 1 echo "Cannot build CppNet" && pause

rem Build CoreCLR version
msbuild /nologo /p:Configuration=ReleaseCoreCLR CppNet.sln
if ERRORLEVEL 1 echo "Cannot build CppNet for CoreCLR" && pause

rem Build Store version
msbuild /nologo /p:Configuration=Release CppNet_Store.sln
if ERRORLEVEL 1 echo "Cannot build CppNet for Store" && pause

popd

xcopy /Y /Q %CPPNET%\Bin\Release\CppNet.dll . > nul
if ERRORLEVEL 1  echo "Cannot copy CppNet.dll" && pause
xcopy /Y /Q %CPPNET%\Bin\Release\CppNet.pdb . > nul
if ERRORLEVEL 1  echo "Cannot copy CppNet.pdb" && pause

xcopy /Y /Q %CPPNET%\Bin\CoreCLR\Release\CppNet.dll CoreCLR\ > nul
if ERRORLEVEL 1  echo "Cannot copy CppNet.dll to CoreCLR" && pause
xcopy /Y /Q %CPPNET%\Bin\CoreCLR\Release\CppNet.pdb CoreCLR\ > nul
if ERRORLEVEL 1  echo "Cannot copy CppNet.pdb to CoreCLR" && pause

xcopy /Y /Q %CPPNET%\Bin\Store\Release\CppNet.dll Store\ > nul
if ERRORLEVEL 1  echo "Cannot copy CppNet.dll to Store" && pause
xcopy /Y /Q %CPPNET%\Bin\Store\Release\CppNet.pdb Store\ > nul
if ERRORLEVEL 1  echo "Cannot copy CppNet.pdb to Store" && pause

echo CppNet build completed successfully
