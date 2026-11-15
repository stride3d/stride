setlocal
set STRIDE_PATH=%~dp0..\..\..
set LAUNCHER_PATH=%~dp0bin\Debug\publish
dotnet msbuild "%STRIDE_PATH%\build\package.proj" -t:_PackNuspec -nologo "-p:StrideNuspec=%~dp0Stride.Launcher.nuspec" "-p:StrideNuspecBasePath=%LAUNCHER_PATH%" "-p:StrideNuspecProperties=SetupUrl=https://github.com/stride3d/stride/releases%%3BUpdateRules=setup=https://github.com/stride3d/stride/releases" "-p:StrideNuspecOutput=%LAUNCHER_PATH%"
