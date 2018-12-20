@echo off

rem If not already loaded, setup VisualStudio
call "%ProgramFiles(x86)%\Microsoft Visual Studio 14.0\vc\vcvarsall.bat" x86

set opentk=..\..\externals\opentk

pushd %opentk%
..\..\build\.nuget\NuGet.exe  restore OpenTK.sln
..\..\build\.nuget\NuGet.exe  restore source\OpenTK\OpenTK.NETStandard.csproj
popd

REM NET Standard
msbuild %opentk%\source\OpenTK\OpenTK.NETStandard.csproj /Property:Configuration=Release;Platform="AnyCPU"
copy /Y %opentk%\Binaries\OpenTK\Release\netstandard2.0\OpenTK.dll .
copy /Y %opentk%\Binaries\OpenTK\Release\netstandard2.0\OpenTK.pdb .

REM Android
msbuild %opentk%\OpenTK.Android.sln /Property:Configuration=Release;Platform="Any CPU"
if not exist Android mkdir Android
copy /Y %opentk%\Binaries\Android\Release\OpenTK-1.1.dll Android
copy /Y %opentk%\Binaries\Android\Release\OpenTK-1.1.dll.mdb Android

REM iOS
msbuild %opentk%\OpenTK.iOS.sln /Property:Configuration=Release;Platform="Any CPU"
if not exist iOS mkdir iOS
copy /Y %opentk%\Binaries\iOS\Release\OpenTK-1.1.dll iOS
copy /Y %opentk%\Binaries\iOS\Release\OpenTK-1.1.dll.mdb iOS
