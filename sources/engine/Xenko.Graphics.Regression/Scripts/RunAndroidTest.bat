@echo off
REM Parameters
REM %1 server IP
REM %2 server listening port
REM %3 build number
REM %4 device serial

GOTO skipLoop

REM %%D is the device, loop through all of them
FOR /F "skip=1" %%D IN ('adb devices') DO (

    REM kill previous instance (does not work on android prior to 3.0 - Honeycomb)
    adb -s %%D am shell force-stop Xenko.Graphics.RegressionTests

    C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe C:\Projects\Xenko\sources\engine\Xenko.Graphics.RegressionTests\Xenko.Graphics.RegressionTests.Android.csproj /p:SolutionName=Xenko.Android;SolutionDir=C:\Projects\Xenko\ /t:Install /p:AdbTarget="-s %%D"

    REM install the package -> should be done by bamboo too?
    REM adb -s %%D -d install -r C:\Projects\Xenko\Bin\Android-AnyCPU-OpenGLES\Xenko.Graphics.RegressionTests-Signed.apk

    REM run it
    adb -s %%D shell am start -a android.intent.action.MAIN -n Xenko.Graphics.RegressionTests/xenko.graphics.regressiontests.Program -e XENKO_SERVER_IP %1 -e XENKO_SERVER_PORT %2 -e XENKO_BUILD_NUMBER %3
)

:skipLoop

adb -s %4 am shell force-stop Xenko.Graphics.RegressionTests

C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe C:\Projects\Xenko\sources\engine\Xenko.Graphics.RegressionTests\Xenko.Graphics.RegressionTests.Android.csproj /p:SolutionName=Xenko.Android;SolutionDir=C:\Projects\Xenko\ /t:Install /p:AdbTarget="-s %4"

REM install the package -> should be done by bamboo too?
REM adb -s %%D -d install -r C:\Projects\Xenko\Bin\Android-AnyCPU-OpenGLES\Xenko.Graphics.RegressionTests-Signed.apk

REM run it
adb -s %4 shell am start -a android.intent.action.MAIN -n Xenko.Graphics.RegressionTests/xenko.graphics.regressiontests.Program -e XENKO_SERVER_IP %1 -e XENKO_SERVER_PORT %2 -e XENKO_BUILD_NUMBER %3 -e XENKO_DEVICE_SERIAL %4