@echo off
setlocal
set StrideSdkDir=%~dp0..\..\..\..\
set StrideAssetCompiler=%StrideSdkDir%sources\assets\Stride.AssetCompiler\bin\Debug\net10.0\Stride.AssetCompiler.exe
rmdir /s /q %~dp0obj\ 2>nul
mkdir %~dp0obj

rem The asset compiler loads the session from a .sdbuild manifest. There is no project here, so write one
rem that points at the package and lists the engine shaders as project assets of Stride.Graphics.
set Manifest=%~dp0obj\Graphics.sdbuild
(
  echo !AssetBuildManifest
  echo Version: 1
  echo ProjectFile: "../../Stride.Graphics.csproj"
  echo PackageFile: "../Graphics.sdpkg"
  echo ProjectAssets:
  for %%f in (%~dp0..\Shaders\*.sdsl %~dp0..\Shaders\*.sdfx) do echo     - Path: "../../Shaders/%%~nxf"
) > %Manifest%

set Options=--platform=Windows --disable-auto-compile --output-path=%~dp0obj\app_data --build-path=%~dp0obj\build_app_data
%StrideAssetCompiler% build %Manifest% %Options% --property:StrideGraphicsApi=Direct3D11
%StrideAssetCompiler% build %Manifest% %Options% --property:StrideGraphicsApi=Direct3D12
%StrideAssetCompiler% build %Manifest% %Options% --property:StrideGraphicsApi=Vulkan
