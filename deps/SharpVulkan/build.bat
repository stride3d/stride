@echo off

if "%1" == "" (
	echo Missing Debug or Release argument
	EXIT /B 1
)

pushd ..\..\externals\SharpVulkan\Source\SharpVulkan

REM SDL2-CS
call "%PROGRAMFILES(X86)%\Microsoft Visual Studio\2017\Community\Common7\Tools\VsMSBuildCmd.bat"
msbuild /p:Configuration="%1" /p:SharpVulkanPlatformDefine=PLATFORM_WINDOWS /p:SharpVulkanPlatformName=Windows SharpVulkan.csproj /restore
if %ERRORLEVEL% neq 0 (
	echo Error during compilation
	popd
	EXIT /B %ERRORLEVEL%
)

msbuild /p:Configuration="%1" /p:SharpVulkanPlatformDefine=PLATFORM_MACOS /p:SharpVulkanPlatformName=macOS SharpVulkan.csproj /restore
if %ERRORLEVEL% neq 0 (
	echo Error during compilation
	popd
	EXIT /B %ERRORLEVEL%
)

msbuild /p:Configuration="%1" /p:SharpVulkanPlatformName=Other SharpVulkan.csproj /restore
if %ERRORLEVEL% neq 0 (
	echo Error during compilation
	popd
	EXIT /B %ERRORLEVEL%
)

popd

rem Copying assemblies
copy ..\..\externals\SharpVulkan\Source\SharpVulkan\bin\%1\Windows\*.* Windows
copy ..\..\externals\SharpVulkan\Source\SharpVulkan\bin\%1\macOS\*.* macOS
copy ..\..\externals\SharpVulkan\Source\SharpVulkan\bin\%1\Other\*.* Other
