pushd ..\..\..\bin\iOS-OpenGLES
..\..\deps\monolinker\monolinker -a Xenko.Engine.dll -a Xenko.Games.dll -a Xenko.Input.dll -a Xenko.Graphics.dll -p link OpenTK-1.1 -u copy -b true -d "C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\Xamarin.iOS\v1.0"
popd

copy ..\..\..\bin\iOS-OpenGLES\output\OpenTK-1.1.dll .