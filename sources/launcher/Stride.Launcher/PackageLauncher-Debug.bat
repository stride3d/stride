setlocal
set STRIDE_PATH=%~dp0..\..\..
set NUGET=%STRIDE_PATH%\build\.nuget\Nuget.exe
set LAUNCHER_PATH=%~dp0bin\Debug\publish
pushd %LAUNCHER_PATH%
%NUGET% pack %~dp0Stride.Launcher.nuspec -BasePath %LAUNCHER_PATH% -Properties "SetupUrl=https://github.com/stride3d/stride/releases;UpdateRules=setup=https://github.com/stride3d/stride/releases"
popd