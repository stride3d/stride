@echo OFF
set recast_source=%~dp0..\..\externals\recast
set output=%~dp0
set vcvarsall="C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Auxiliary\Build\vcvarsall.bat"

REM copy project files
copy /Y Detour.vcxproj "%recast_source%\Detour.vcxproj"
copy /Y Recast.vcxproj "%recast_source%\Recast.vcxproj"

REM build x64 projects
call %vcvarsall% x64
cd "%output%"
msbuild "%recast_source%\Detour.vcxproj" /p:Platform="x64";Configuration="Release"
msbuild "%recast_source%\Recast.vcxproj" /p:Platform="x64";Configuration="Release"

REM build x86 projects
call %vcvarsall% x86
cd "%output%"
msbuild "%recast_source%\Detour.vcxproj" /p:Platform="x86";Configuration="Release"
msbuild "%recast_source%\Recast.vcxproj" /p:Platform="x86";Configuration="Release"

REM build ARM64 projects
call %vcvarsall% arm64
cd "%output%"
msbuild "%recast_source%\Detour.vcxproj" /p:Platform="ARM64";Configuration="Release"
msbuild "%recast_source%\Recast.vcxproj" /p:Platform="ARM64";Configuration="Release"

REM copy include files (some additional include files are needed for post-processing)
rmdir /Q /S "%output%include"
mkdir "%output%include\"
copy /Y "%recast_source%\Detour\Include\*.*" 			"%output%include"
copy /Y "%recast_source%\Recast\Include\*.*" 			"%output%include"

REM copy libs in NativePath
xcopy /Y "%recast_source%\lib"	"%~dp0..\NativePath\Windows\"

REM for information and redistribution, copy license & credits
copy /Y %recast_source%\License.txt "%output%"
cd %output%