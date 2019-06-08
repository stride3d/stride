@echo OFF
REM Build Assimp using Cmake
setlocal
set CMAKE_BIN=cmake.exe

set assimp_source=%~dp0..\..\externals\assimp
set x86_output=%assimp_source%\build\x86
set x64_output=%assimp_source%\build\x64
set output=%~dp0

REM generate x64 projects
rmdir /Q /S "%x64_output%"
mkdir "%x64_output%"
del "%assimp_source%\CMakeCache.txt"
pushd "%x64_output%"
"%CMAKE_BIN%" -Wno-dev -G "Visual Studio 12 Win64" --build "%assimp_source%"
popd

REM generate x86 projects
rmdir /Q /S "%x86_output%"
mkdir "%x86_output%"
del "%assimp_source%\CMakeCache.txt"
pushd "%x86_output%"
"%CMAKE_BIN%" -Wno-dev -G "Visual Studio 12" --build "%assimp_source%"
popd

REM build x64 projects
call "%ProgramFiles(x86)%\Microsoft Visual Studio 14.0\vc\vcvarsall.bat" x64
cd "%output%"
msbuild "%x64_output%\code\assimp.vcxproj" /p:Platform="x64";Configuration="Release"

REM build x86 projects
call "%ProgramFiles(x86)%\Microsoft Visual Studio 14.0\vc\vcvarsall.bat" x86
cd "%output%"
msbuild "%x86_output%\code\assimp.vcxproj" /p:Platform="x86";Configuration="Release"

REM copy include files (some additional include files are needed for post-processing)
rmdir /Q /S "%output%include"
mkdir "%output%include\assimp\Compiler"
copy /Y "%assimp_source%\include\assimp\*.*" 			"%output%include\assimp"
copy /Y "%assimp_source%\include\assimp\Compiler\*.*" 	"%output%include\assimp\Compiler"
copy /Y "%assimp_source%\code\Importer.h" 				"%output%include\assimp"
copy /Y "%assimp_source%\code\BaseProcess.h"			"%output%include\assimp"
copy /Y "%assimp_source%\code\GenericProperty.h"		"%output%include\assimp"
copy /Y "%assimp_source%\code\Hash.h"					"%output%include\assimp"

REM copy dlls in deps
rmdir /Q /S "%output%bin"
mkdir "%output%bin\assimp_release-dll_Win32"
mkdir "%output%bin\assimp_release-dll_x64"
copy /Y "%x86_output%\code\Release\*.dll"	"%output%bin\assimp_release-dll_Win32"
copy /Y "%x64_output%\code\Release\*.dll"	"%output%bin\assimp_release-dll_x64"

REM copy libs in deps
rmdir /Q /S "%output%lib"
mkdir "%output%lib\assimp_release-dll_Win32"
mkdir "%output%lib\assimp_release-dll_x64"
copy /Y "%x86_output%\code\Release\*.lib"	"%output%lib\assimp_release-dll_Win32"
copy /Y "%x64_output%\code\Release\*.lib"	"%output%lib\assimp_release-dll_x64"

REM for information and redistribution, copy license & credits
copy /Y ..\..\externals\assimp\LICENSE "%output%"
cd %output%
