@echo off
REM TODO: Space in --compile-property is not well supported, so a xenko path without space is currently required
set XenkoBuildFolder=%~dp0..\..\..\..\build
set XenkoBinFolder=%~dp0..\..\..\..\Bin\Windows
set XenkoAssetCompiler=%XenkoBinFolder%\Xenko.Core.Assets.CompilerApp.exe

%XenkoAssetCompiler% --platform=Windows --graphics-platform=Direct3D11 --profile=Windows --disable-auto-compile --project-configuration=Debug --compile-property:SolutionDir=%XenkoBuildFolder%\;SolutionName=Xenko;BuildProjectReferences=false --output-path=outputpath --build-path=buildpath --package-file=EditorPackage.xkpkg

mkdir %XenkoBinFolder%\editor
copy outputpath\db\bundles\EditorShadersD3D11.bundle %XenkoBinFolder%\editor