@echo off

rem If not already loaded, setup VisualStudio
if "%VisualStudioVersion%" EQ "" call "%ProgramFiles(x86)%\Microsoft Visual Studio 14.0\vc\vcvarsall.bat" x86

msbuild ..\..\externals\opentk\OpenTK.sln /Property:Configuration=Debug;Platform="Any CPU"
copy ..\..\externals\opentk\Binaries\OpenTK\Debug\OpenTK.dll .
copy ..\..\externals\opentk\Binaries\OpenTK\Debug\OpenTK.pdb .
copy ..\..\externals\opentk\Binaries\OpenTK\Debug\OpenTK.GLControl.dll .
copy ..\..\externals\opentk\Binaries\OpenTK\Debug\OpenTK.GLControl.pdb .
