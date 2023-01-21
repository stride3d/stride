call "%ProgramFiles(x86)%\Microsoft Visual Studio 14.0\vc\vcvarsall.bat" x86
msbuild ..\..\externals\freetype\builds\windows\vc2013\freetype.vcxproj /Property:Configuration=Release;StridePlatform=Windows;Platform=Win32
msbuild ..\..\externals\freetype\builds\windows\vc2013\freetype.vcxproj /Property:Configuration=Release;StridePlatform=Windows;Platform=x64

xcopy /Y /S ..\..\externals\freetype\builds\windows\vc2013\bin\Windows\Release\*.dll Windows\
xcopy /Y /S ..\..\externals\freetype\builds\windows\vc2013\bin\Windows\Release\*.pdb Windows\
