@echo off

echo Processing Runtime (currently using Linux as template)
..\sources\tools\Stride.ProjectGenerator\bin\Debug\net472\Stride.ProjectGenerator.exe solution Stride.sln -o Stride.Runtime.sln -p Linux
echo.

echo Processing Android
..\sources\tools\Stride.ProjectGenerator\bin\Debug\net472\Stride.ProjectGenerator.exe solution Stride.sln -o Stride.Android.sln -p Android
echo.

echo Processing iOS
..\sources\tools\Stride.ProjectGenerator\bin\Debug\net472\Stride.ProjectGenerator.exe solution Stride.sln -o Stride.iOS.sln -p iOS
echo.
