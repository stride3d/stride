CALL "%VS2017%\Common7\Tools\VsDevCmd.bat"
msbuild Xenko.build /p:XenkoGenerateDoc=true /t:BuildWindows > NUL