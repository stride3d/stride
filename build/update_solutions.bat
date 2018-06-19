@echo off

echo Processing Windows
..\Bin\Windows\Xenko.ProjectGenerator.exe solution Xenko.sln -o Xenko.Direct3D.sln -p Windows
echo.

echo Processing Windows with CoreCLR
..\Bin\Windows\Xenko.ProjectGenerator.exe solution Xenko.sln -o Xenko.Direct3D.CoreCLR.sln -p Windows 
echo.

echo Processing Windows with SDL
..\Bin\Windows\Xenko.ProjectGenerator.exe solution Xenko.sln -o Xenko.Direct3D.SDL.sln -p Windows
echo.

echo Processing Windows with a Null graphic backend
..\Bin\Windows\Xenko.ProjectGenerator.exe solution Xenko.sln -o Xenko.Null.sln -p Windows
echo.

echo Processing Windows with Vulkan
..\Bin\Windows\Xenko.ProjectGenerator.exe solution Xenko.sln -o Xenko.Vulkan.sln -p Windows 
echo.

echo Processing Windows with Direct3D12
..\Bin\Windows\Xenko.ProjectGenerator.exe solution Xenko.sln -o Xenko.Direct3D12.sln -p Windows
echo.

echo Processing macOS
..\Bin\Windows\Xenko.ProjectGenerator.exe solution Xenko.sln -o Xenko.macOS.sln -p macOS
echo.

echo Processing macOS with CoreCLR
..\Bin\Windows\Xenko.ProjectGenerator.exe solution Xenko.sln -o Xenko.macOS.CoreCLR.sln -p macOS
echo.

echo Processing Linux
..\Bin\Windows\Xenko.ProjectGenerator.exe solution Xenko.sln -o Xenko.Linux.sln -p Linux
echo.

echo Processing Linux with Vulkan
..\Bin\Windows\Xenko.ProjectGenerator.exe solution Xenko.sln -o Xenko.Linux.Vulkan.sln -p Linux
echo.

echo Processing Linux with CoreCLR
..\Bin\Windows\Xenko.ProjectGenerator.exe solution Xenko.sln -o Xenko.Linux.CoreCLR.sln -p Linux
echo.

echo Processing Linux with Vulkan with CoreCLR
..\Bin\Windows\Xenko.ProjectGenerator.exe solution Xenko.sln -o Xenko.Linux.Vulkan.CoreCLR.sln -p Linux
echo.

echo Processing Windows with OpenGL
..\Bin\Windows\Xenko.ProjectGenerator.exe solution Xenko.sln -o Xenko.OpenGL.sln -p Windows
echo.

echo Processing Windows with OpenGL with CoreCLR
..\Bin\Windows\Xenko.ProjectGenerator.exe solution Xenko.sln -o Xenko.OpenGL.CoreCLR.sln -p Windows
echo.

echo Processing Windows with OpenGLES
..\Bin\Windows\Xenko.ProjectGenerator.exe solution Xenko.sln -o Xenko.OpenGLES.sln -p Windows
echo.

echo Processing Android
..\Bin\Windows\Xenko.ProjectGenerator.exe solution Xenko.sln -o Xenko.Android.sln -p Android
echo.

echo Processing iOS
..\Bin\Windows\Xenko.ProjectGenerator.exe solution Xenko.sln -o Xenko.iOS.sln -p iOS
echo.

echo Processing UWP
..\Bin\Windows\Xenko.ProjectGenerator.exe solution Xenko.sln -o Xenko.UWP.sln -p UWP
echo.
