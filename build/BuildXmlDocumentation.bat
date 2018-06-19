CALL "C:\Program Files (x86)\Microsoft Visual Studio\2017\Professional\Common7\Tools\VsDevCmd.bat"
msbuild Xenko.build /p:XenkoGenerateDoc=true /t:BuildWindows > NUL

