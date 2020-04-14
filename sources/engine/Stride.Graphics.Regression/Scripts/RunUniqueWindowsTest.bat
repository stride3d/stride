@echo off
REM Parameters
REM %1 server IP
REM %2 server listening port
REM %3 build number
REM %4 serial
REM %5 test name
REM %6 graphics API

if %6 EQU PC_Direct3D11 (
    REM build DirectX
    REM C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe %StrideSdkDir%\sources\engine\Stride.Graphics.RegressionTests\Stride.Graphics.RegressionTests.csproj /p:SolutionName=Stride;SolutionDir=%StrideSdkDir%\ /t:Build

    REM run DirectX
    start Bin\Windows-AnyCPU-Direct3D\Stride.Graphics.RegressionTests.exe %1 %2 %3 %4 %5
) else if %6 EQU PC_OpenGL (
    REM build OpenGL
    REM C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe %StrideSdkDir%\sources\engine\Stride.Graphics.RegressionTests\Stride.Graphics.RegressionTests.csproj /p:SolutionName=Stride.OpenGL;SolutionDir=%StrideSdkDir%\ /t:Build

    REM run OpenGL
    start Bin\Windows-AnyCPU-OpenGL\Stride.Graphics.RegressionTests.exe %1 %2 %3 %4 %5
) else if %6 EQU PC_OpenGLES (
    REM build OpenGL ES
    REM C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe %StrideSdkDir%\sources\engine\Stride.Graphics.RegressionTests\Stride.Graphics.RegressionTests.csproj /p:SolutionName=Stride.OpenGLES;SolutionDir=%StrideSdkDir%\ /t:Build
    
    REM run OpenGL ES
    start Bin\Windows-AnyCPU-OpenGLES\Stride.Graphics.RegressionTests.exe %1 %2 %3 %4 %5
)
