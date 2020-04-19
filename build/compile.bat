@echo off

setlocal

set STARTTIME=%TIME%
set __SkipTestBuild=true
set __BuildType=Debug
set __BuildVerbosity=m
set __BuildDoc=0
set __ContinueOnError=false

:Arg_Loop
rem This does not check for duplicate arguments, the last one will take precedence
if "%1" == "" goto ArgsDone
if /i "%1" == "/?" goto Usage
if /i "%1" == "debug" (set __BuildType=Debug && shift && goto Arg_loop)
if /i "%1" == "release" (set __BuildType=Release && shift && goto Arg_loop)
if /i "%1" == "tests" (set __SkipTestBuild=false && shift && goto Arg_loop)
if /i "%1" == "continueonerror" (set __ContinueOnError=true && shift && goto Arg_loop)
if /i "%1" == "verbosity:q" (set __BuildVerbosity=q && shift && goto Arg_loop)
if /i "%1" == "verbosity:m" (set __BuildVerbosity=m && shift && goto Arg_loop)
if /i "%1" == "verbosity:n" (set __BuildVerbosity=n && shift && goto Arg_loop)
if /i "%1" == "verbosity:d" (set __BuildVerbosity=d && shift && goto Arg_loop)
if /i "%1" == "doc" (set __BuildDoc=1 && shift && goto Arg_loop)
rem No space after %2 as it would add a space at the end of __SelectedProject
if /i "%1" == "project" (if "%2" == "" (goto Usage) else (set __SelectedProject=%2&& shift && shift && goto Arg_loop))
echo.
echo Invalid command line argument: %1
echo.
goto Usage

:Usage
echo compile.bat [/? ^| debug ^| release ^| tests ^| verbosity:[q^|m^|n^|d] ^| project Project.sln
echo.
echo   debug   : Build debug version
echo   release : Build release version
echo   tests   : Build tests
echo verbosity : Verbosity level [q]uiet, [m]inimal, [n]ormal or [d]iagnostic. Default is [m]inimal
echo   project : Chosen project
echo.

goto exit

:ArgsDone

rem Let's check we have msbuild in our path
msbuild /nologo /version > nul 2>&1
if %ERRORLEVEL% NEQ 0 (
    echo Cannot find msbuild.
    goto exit
)
rem Check that msbuild is version 15 or greater
for /f "tokens=1 delims=." %%i in ('msbuild /nologo /version') do set __BuildVersion=%%i

if %__BuildVersion% LSS 15 (
    echo MSbuild version 15 or greater is required
    goto exit
)

set XXMSBUILD=msbuild.exe
set _platform_target=Mixed Platforms

rem Compiling the various solutions

set Project=Stride.sln
rem We always compile tests for the main solution
set __OldSkipTestBuild=%__SkipTestBuild%
set __SkipTestBuild=false
call :compile
set __SkipTestBuild=%__OldSkipTestBuild%
if %ERRORLEVEL% NEQ 0 if "%__ContinueOnError%" == "false" goto exit

set Project=Stride.Direct3D.sln
call :compile
if %ERRORLEVEL% NEQ 0 if "%__ContinueOnError%" == "false" goto exit

set Project=Stride.Direct3D.SDL.sln
call :compile
if %ERRORLEVEL% NEQ 0 if "%__ContinueOnError%" == "false" goto exit

set Project=Stride.Direct3D.CoreCLR.sln
call :compile
if %ERRORLEVEL% NEQ 0 if "%__ContinueOnError%" == "false" goto exit

set Project=Stride.Direct3D12.sln
call :compile
if %ERRORLEVEL% NEQ 0 if "%__ContinueOnError%" == "false" goto exit

set Project=Stride.Null.sln
call :compile
if %ERRORLEVEL% NEQ 0 if "%__ContinueOnError%" == "false" goto exit

set Project=Stride.Vulkan.sln
call :compile
if %ERRORLEVEL% NEQ 0 if "%__ContinueOnError%" == "false" goto exit

set Project=Stride.Vulkan.SDL.sln
call :compile
if %ERRORLEVEL% NEQ 0 if "%__ContinueOnError%" == "false" goto exit

set Project=Stride.OpenGL.sln
call :compile
if %ERRORLEVEL% NEQ 0 if "%__ContinueOnError%" == "false" goto exit

set Project=Stride.OpenGL.CoreCLR.sln
call :compile
if %ERRORLEVEL% NEQ 0 if "%__ContinueOnError%" == "false" goto exit

set Project=Stride.Linux.sln
set _platform_target=Linux
call :compile
if %ERRORLEVEL% NEQ 0 if "%__ContinueOnError%" == "false" goto exit

set Project=Stride.Linux.Vulkan.sln
call :compile
if %ERRORLEVEL% NEQ 0 if "%__ContinueOnError%" == "false" goto exit

set Project=Stride.Linux.CoreCLR.sln
call :compile
if %ERRORLEVEL% NEQ 0 if "%__ContinueOnError%" == "false" goto exit

set Project=Stride.Linux.Vulkan.CoreCLR.sln
call :compile
if %ERRORLEVEL% NEQ 0 if "%__ContinueOnError%" == "false" goto exit

set Project=Stride.macOS.sln
set _platform_target=macOS
call :compile
if %ERRORLEVEL% NEQ 0 if "%__ContinueOnError%" == "false" goto exit

set Project=Stride.macOS.CoreCLR.sln
call :compile
if %ERRORLEVEL% NEQ 0 if "%__ContinueOnError%" == "false" goto exit

set Project=Stride.Android.sln
set _platform_target=Android
call :compile
if %ERRORLEVEL% NEQ 0 if "%__ContinueOnError%" == "false" goto exit

set Project=Stride.iOS.sln
set _platform_target=iPhone
call :compile
if %ERRORLEVEL% NEQ 0 if "%__ContinueOnError%" == "false" goto exit

set Project=Stride.UWP.sln
set _platform_target=UWP
call :compile
if %ERRORLEVEL% NEQ 0 if "%__ContinueOnError%" == "false" goto exit

goto exit

rem Compile our solution. The following variables needs to be set:
rem "Project" is the solution name
rem "_platform_target" is the platform being targeted
:compile
set _option=/nologo /nr:false /m /verbosity:%__BuildVerbosity% /p:Configuration=%__BuildType% /p:Platform="%_platform_target%" /p:StrideSkipUnitTests=%__SkipTestBuild% %Project%

if "%__BuildDoc%" == "1" set _option=%_option% /p:StrideGenerateDoc=true

rem Skip Compilation if __SelectedProject was set and does not match what was requested
if "%__SelectedProject%" NEQ "" (
    if "%__SelectedProject%" NEQ "%Project%" (
        goto :eof
    )
)

echo Compiling using command line %XXMSBUILD% %_option%
echo.

rem Launch the build and checkling for an error
%XXMSBUILD%  %_option%
if %ERRORLEVEL% NEQ 0 (
    echo Error while compiling project: %Project%
    echo Command line was: %XXMSBUILD% %_option%
    exit /b 1
) else (
    echo Done compiling project: %Project%
)
echo.
goto :eof

:exit

set ENDTIME=%TIME%

echo.
echo Starting time was: %STARTTIME%
echo Ending time is   : %ENDTIME%

rem convert STARTTIME and ENDTIME to miliseconds
rem The format of %TIME% is HH:MM:SS,CS for example 23:59:59,99
set /A STARTTIME=(1%STARTTIME:~0,2%-100)*3600000 + (1%STARTTIME:~3,2%-100)*60000 + (1%STARTTIME:~6,2%-100)*1000 + (1%STARTTIME:~9,2%-100)*10
set /A ENDTIME=(1%ENDTIME:~0,2%-100)*3600000 + (1%ENDTIME:~3,2%-100)*60000 + (1%ENDTIME:~6,2%-100)*1000 + (1%ENDTIME:~9,2%-100)*10

rem calculating the duration is easy
set /A DURATION=%ENDTIME%-%STARTTIME%

rem we might have measured the time inbetween days
if %ENDTIME% LSS %STARTTIME% set set /A DURATION=%STARTTIME%-%ENDTIME%

set /A DURATION=%DURATION%/1000

if %DURATION% GEQ 60 (
    set /A MINUTES=%DURATION% / 60
    rem Get rid of the part after the .
    for /f "tokens=1,2 delims=." %%a  in ("%MINUTES%") do set MINUTES=%%a
    set /A DURATION=%DURATION%%%60
) else (
    set /A MINUTES=0
)

rem outputing
if %MINUTES% NEQ 0 (
    echo Duration is      : %MINUTES% minutes and %DURATION% seconds
) else (
    echo Duration is      : %DURATION% seconds
)

endlocal

@echo on
