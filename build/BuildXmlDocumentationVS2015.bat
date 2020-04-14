CALL "%VS140COMNTOOLS%VsDevCmd.bat"
msbuild Stride.build /p:StrideGenerateDoc=true /t:BuildWindows > NUL

