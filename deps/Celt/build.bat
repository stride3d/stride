@echo off

echo "Building OPUS codec"

set OLD_PATH=%PATH%
set PATH=%~dp0..\LLVM;%PATH%

pushd ..\..\externals\Celt
set CELT_DIR=%cd%
popd
pushd ..\..\externals\NativePath\Tools
set NATIVE_PATH_DIR=%cd%
popd

set LUA=C:\apps\luapower-all-master

set ARGUMENTS=%NATIVE_PATH_DIR%\np-build.lua %CELT_DIR%\celt -I%CELT_DIR%\include -I%CELT_DIR%\celt -E%CELT_DIR%\celt\arm -E%CELT_DIR%\celt\dump_modes -E%CELT_DIR%\celt\mips -E%CELT_DIR%\celt\tests -E%CELT_DIR%\celt\x86 -e%CELT_DIR%\celt\opus_custom_demo.c -DUSE_ALLOCA -DHAVE_LRINTF -DCUSTOM_MODES -DOPUS_BUILD -nlibCelt

pushd ..\NativePath
call %LUA%\luajit %ARGUMENTS% -pwindows
call %LUA%\luajit %ARGUMENTS% -pandroid
call %LUA%\luajit %ARGUMENTS% -pios
call %LUA%\luajit %ARGUMENTS% -pmacos
call %LUA%\luajit %ARGUMENTS% -plinux
popd

set CELT_DIR=
set NATIVE_PATH_DIR=
set PATH=%OLD_PATH%
