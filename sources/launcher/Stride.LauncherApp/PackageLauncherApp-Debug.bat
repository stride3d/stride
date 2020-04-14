setlocal
set STRIDE_PATH=%~dp0..\..\..
set NUGET=%STRIDE_PATH%\build\.nuget\Nuget.exe
set ILREPACK=%STRIDE_PATH%\Bin\Windows\ILRepack.exe
set LAUNCHER_PATH=%~dp0Bin\Debug
pushd %LAUNCHER_PATH%
%NUGET% pack %~dp0Stride.LauncherApp.nuspec -BasePath %LAUNCHER_PATH%
popd