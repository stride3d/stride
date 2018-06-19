REM @echo off
setlocal

set MSDFGEN=%~dp0..\..\externals\msdfgen

REM goto :CopyOutput

set TINYXML2=%~dp0..\..\externals\tinyxml2
set LODEPNG=%~dp0..\..\externals\lodepng

set "INCLUDE=%TINYXML2%;%INCLUDE%"
set "LIB=%TINYXML2%;%LIB%"

set "INCLUDE=%LODEPNG%;%INCLUDE%"
set "LIB=%LODEPNG%;%LIB%"

set UseEnv=true

call "%ProgramFiles(x86)%\Microsoft Visual Studio 14.0\vc\vcvarsall.bat" x86
msbuild /tv:14.0 /t:Build /verbosity:quiet /clp:ErrorsOnly /fl /flp:logfile=BuildErrors.log;ErrorsOnly "/p:Configuration=Release;Platform=x86" %MSDFGEN%\Msdfgen.sln

:CopyOutput
xcopy %MSDFGEN%\msdfgen.exe . /Y
xcopy %MSDFGEN%\LICENSE.txt . /Y

