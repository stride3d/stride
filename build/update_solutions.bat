@echo off

echo Processing Runtime (currently using Linux as template)
..\sources\tools\Xenko.ProjectGenerator\bin\Debug\net472\Xenko.ProjectGenerator.exe solution Xenko.sln -o Xenko.Runtime.sln -p Linux
echo.

echo Processing Android
..\sources\tools\Xenko.ProjectGenerator\bin\Debug\net472\Xenko.ProjectGenerator.exe solution Xenko.sln -o Xenko.Android.sln -p Android
echo.

echo Processing iOS
..\sources\tools\Xenko.ProjectGenerator\bin\Debug\net472\Xenko.ProjectGenerator.exe solution Xenko.sln -o Xenko.iOS.sln -p iOS
echo.
