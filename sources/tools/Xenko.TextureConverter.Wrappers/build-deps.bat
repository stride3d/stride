call "C:\Program Files (x86)\Microsoft Visual Studio 14.0\vc\vcvarsall.bat" x86
msbuild Xenko.TextureConverter.Wrappers.sln /Property:Configuration=Debug;Platform="Win32"
msbuild Xenko.TextureConverter.Wrappers.sln /Property:Configuration=Release;Platform="Win32"
msbuild Xenko.TextureConverter.Wrappers.sln /Property:Configuration=Debug;Platform="x64"
msbuild Xenko.TextureConverter.Wrappers.sln /Property:Configuration=Release;Platform="x64"