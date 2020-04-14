@echo off
REM Parameters
REM %1 server IP
REM %2 server listening port
REM %3 build number
REM %4 device serial
REM %5 test name
REM %6 assembly name
REM %7 csproj location

REM kill previous instance (does not work on android prior to 3.0 - Honeycomb)
adb -s %4 am shell force-stop %6

C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe %XenkoSdkDir%\%7 /p:SolutionName=Xenko.Android /p:SolutionDir=%XenkoSdkDir%\ /p:Configuration=Release /t:Install /p:AdbTarget="-s %4"

REM run it
adb -s %4 shell am start -a android.intent.action.MAIN -n %6/xenko.graphicstests.GraphicsTestRunner -e XENKO_SERVER_IP %1 -e XENKO_SERVER_PORT %2 -e XENKO_BUILD_NUMBER %3 -e XENKO_DEVICE_SERIAL %4 -e XENKO_TEST_NAME %5