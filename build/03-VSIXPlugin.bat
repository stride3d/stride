@echo off

CALL "%VS2017%\Common7\Tools\VsDevCmd.bat"
msbuild Xenko.build /t:VSIXPlugin

pause
