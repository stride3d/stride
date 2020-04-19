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
    adb -s %%D am shell force-stop Stride.Graphics.RegressionTests

    C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe C:\Projects\Stride\sources\engine\Stride.Graphics.RegressionTests\Stride.Graphics.RegressionTests.Android.csproj /p:SolutionName=Stride.Android;SolutionDir=C:\Projects\Stride\ /t:Install /p:AdbTarget="-s %%D"

    REM install the package -> should be done by bamboo too?
    REM adb -s %%D -d install -r C:\Projects\Stride\Bin\Android-AnyCPU-OpenGLES\Stride.Graphics.RegressionTests-Signed.apk

    REM run it
    adb -s %%D shell am start -a android.intent.action.MAIN -n Stride.Graphics.RegressionTests/stride.graphics.regressiontests.Program -e STRIDE_SERVER_IP %1 -e STRIDE_SERVER_PORT %2 -e STRIDE_BUILD_NUMBER %3
)

:skipLoop

adb -s %4 am shell force-stop Stride.Graphics.RegressionTests

C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe C:\Projects\Stride\sources\engine\Stride.Graphics.RegressionTests\Stride.Graphics.RegressionTests.Android.csproj /p:SolutionName=Stride.Android;SolutionDir=C:\Projects\Stride\ /t:Install /p:AdbTarget="-s %4"

REM install the package -> should be done by bamboo too?
REM adb -s %%D -d install -r C:\Projects\Stride\Bin\Android-AnyCPU-OpenGLES\Stride.Graphics.RegressionTests-Signed.apk

REM run it
adb -s %4 shell am start -a android.intent.action.MAIN -n Stride.Graphics.RegressionTests/stride.graphics.regressiontests.Program -e STRIDE_SERVER_IP %1 -e STRIDE_SERVER_PORT %2 -e STRIDE_BUILD_NUMBER %3 -e STRIDE_DEVICE_SERIAL %4