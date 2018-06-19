@echo off

rem If not already loaded, setup VisualStudio
if "%VisualStudioVersion%" EQ "" call "%ProgramFiles(x86)%\Microsoft Visual Studio 14.0\vc\vcvarsall.bat" x86

set opentk=..\..\externals\opentk

pushd %opentk%
..\..\build\.nuget\NuGet.exe  restore OpenTK.sln
popd

REM Windows
msbuild %opentk%\OpenTK.sln /Property:Configuration=Release;Platform="Any CPU"
copy /Y %opentk%\Binaries\OpenTK\Release\OpenTK.dll .
copy /Y %opentk%\Binaries\OpenTK\Release\OpenTK.pdb .

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

if not exist CoreCLR mkdir CoreCLR
msbuild %opentk%\source\OpenTK\OpenTK.csproj /Property:Configuration=ReleaseCoreCLR;Platform=AnyCPU
mkdir CoreCLR\Windows
copy /Y %opentk%\Binaries\OpenTK\ReleaseCoreCLR\OpenTK.dll CoreCLR\Windows
copy /Y %opentk%\Binaries\OpenTK\ReleaseCoreCLR\OpenTK.pdb CoreCLR\Windows

msbuild %opentk%\source\OpenTK\OpenTK.csproj /Property:Configuration=ReleaseCoreCLR;Platform=Linux
if not exist CoreCLR\Linux mkdir CoreCLR\Linux
copy /Y %opentk%\Binaries\OpenTK\Linux\ReleaseCoreCLR\OpenTK.dll CoreCLR\Linux
copy /Y %opentk%\Binaries\OpenTK\Linux\ReleaseCoreCLR\OpenTK.pdb CoreCLR\Linux

msbuild %opentk%\source\OpenTK\OpenTK.csproj /Property:Configuration=ReleaseMinimal;Platform=Linux
if not exist Linux mkdir Linux
copy /Y %opentk%\Binaries\OpenTK\Linux\ReleaseMinimal\OpenTK.dll Linux
copy /Y %opentk%\Binaries\OpenTK\Linux\ReleaseMinimal\OpenTK.pdb Linux

msbuild %opentk%\source\OpenTK\OpenTK.csproj /Property:Configuration=ReleaseCoreCLR;Platform=macOS
if not exist CoreCLR\macOS mkdir CoreCLR\macOS
copy /Y %opentk%\Binaries\OpenTK\macOS\ReleaseCoreCLR\OpenTK.dll CoreCLR\macOS
copy /Y %opentk%\Binaries\OpenTK\macOS\ReleaseCoreCLR\OpenTK.pdb CoreCLR\macOS

msbuild %opentk%\source\OpenTK\OpenTK.csproj /Property:Configuration=ReleaseMinimal;Platform=macOS
if not exist macOS mkdir macOS
copy /Y %opentk%\Binaries\OpenTK\macOS\ReleaseMinimal\OpenTK.dll macOS
copy /Y %opentk%\Binaries\OpenTK\macOS\ReleaseMinimal\OpenTK.pdb macOS
