setlocal
set XENKO_PATH=%~dp0..\..\..
set NUGET=%XENKO_PATH%\build\.nuget\Nuget.exe
set ILREPACK=%XENKO_PATH%\Bin\Windows\ILRepack.exe
set LAUNCHER_PATH=%~dp0Bin\Debug
pushd %LAUNCHER_PATH%
%NUGET% pack %~dp0Xenko.LauncherApp.nuspec -BasePath %LAUNCHER_PATH%
popd