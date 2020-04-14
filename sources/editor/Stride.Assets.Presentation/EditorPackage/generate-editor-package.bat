@echo off
REM TODO: Space in --compile-property is not well supported, so a stride path without space is currently required
set StrideBuildFolder=%~dp0..\..\..\..\build
set StrideBinFolder=%~dp0..\..\..\..\Bin\Windows
set StrideAssetCompiler=%StrideBinFolder%\Stride.Core.Assets.CompilerApp.exe

%StrideAssetCompiler% --platform=Windows --graphics-platform=Direct3D11 --profile=Windows --disable-auto-compile --project-configuration=Debug --compile-property:SolutionDir=%StrideBuildFolder%\;SolutionName=Stride;BuildProjectReferences=false --output-path=outputpath --build-path=buildpath --package-file=EditorPackage.sdpkg

mkdir %StrideBinFolder%\editor
copy outputpath\db\bundles\EditorShadersD3D11.bundle %StrideBinFolder%\editor