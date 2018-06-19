CALL "%VS140COMNTOOLS%VsDevCmd.bat"
msbuild Xenko.build /p:XenkoGenerateDoc=true /t:BuildWindows > NUL

