@echo off
setlocal
set StrideSdkDir=%~dp0..\..\..\..\
set StrideAssetCompiler=%StrideSdkDir%sources\assets\Stride.Core.Assets.CompilerApp\bin\Debug\net10.0\Stride.Core.Assets.CompilerApp.exe
rmdir /s %~dp0obj\
%StrideAssetCompiler% --platform=Windows --property:StrideGraphicsApi=Direct3D11 --output-path=%~dp0obj\app_data --build-path=%~dp0obj\build_app_data --package-file=Graphics.sdpkg
%StrideAssetCompiler% --platform=Windows --property:StrideGraphicsApi=OpenGL --output-path=%~dp0obj\app_data --build-path=%~dp0obj\build_app_data --package-file=Graphics.sdpkg
%StrideAssetCompiler% --platform=Windows --property:StrideGraphicsApi=OpenGLES --output-path=%~dp0obj\app_data --build-path=%~dp0obj\build_app_data --package-file=Graphics.sdpkg
%StrideAssetCompiler% --platform=Windows --property:StrideGraphicsApi=Vulkan --output-path=%~dp0obj\app_data --build-path=%~dp0obj\build_app_data --package-file=Graphics.sdpkg
