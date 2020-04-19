call "%ProgramFiles(x86)%\Microsoft Visual Studio 14.0\vc\vcvarsall.bat" x86
msbuild ..\..\externals\freetype\builds\windows\vc2013\freetype.vcxproj /Property:Configuration=Release;StridePlatform=Windows;Platform=Win32
msbuild ..\..\externals\freetype\builds\windows\vc2013\freetype.vcxproj /Property:Configuration=Release;StridePlatform=Windows;Platform=x64
msbuild ..\..\externals\freetype\builds\windows\vc2013\freetype.vcxproj /Property:Configuration=Release;StridePlatform=WindowsStore;Platform=x64
msbuild ..\..\externals\freetype\builds\windows\vc2013\freetype.vcxproj /Property:Configuration=Release;StridePlatform=WindowsStore;Platform=Win32
msbuild ..\..\externals\freetype\builds\windows\vc2013\freetype.vcxproj /Property:Configuration=Release;StridePlatform=WindowsStore;Platform=ARM
msbuild ..\..\externals\freetype\builds\windows\vc2013\freetype.vcxproj /Property:Configuration=Release;StridePlatform=WindowsPhone;Platform=Win32
msbuild ..\..\externals\freetype\builds\windows\vc2013\freetype.vcxproj /Property:Configuration=Release;StridePlatform=WindowsPhone;Platform=ARM
msbuild ..\..\externals\freetype\builds\windows\vc2013\freetype.vcxproj /Property:Configuration=Release;StridePlatform=Windows10;Platform=x64
msbuild ..\..\externals\freetype\builds\windows\vc2013\freetype.vcxproj /Property:Configuration=Release;StridePlatform=Windows10;Platform=Win32
msbuild ..\..\externals\freetype\builds\windows\vc2013\freetype.vcxproj /Property:Configuration=Release;StridePlatform=Windows10;Platform=ARM

xcopy /Y /S ..\..\externals\freetype\builds\windows\vc2013\bin\Windows\Release\*.dll Windows\
xcopy /Y /S ..\..\externals\freetype\builds\windows\vc2013\bin\Windows\Release\*.pdb Windows\
xcopy /Y /S ..\..\externals\freetype\builds\windows\vc2013\bin\Windows10\Release\*.dll Windows10\
xcopy /Y /S ..\..\externals\freetype\builds\windows\vc2013\bin\Windows10\Release\*.pdb Windows10\
xcopy /Y /S ..\..\externals\freetype\builds\windows\vc2013\bin\WindowsPhone\Release\*.dll WindowsPhone\
xcopy /Y /S ..\..\externals\freetype\builds\windows\vc2013\bin\WindowsPhone\Release\*.pdb WindowsPhone\
xcopy /Y /S ..\..\externals\freetype\builds\windows\vc2013\bin\WindowsStore\Release\*.dll WindowsStore\
xcopy /Y /S ..\..\externals\freetype\builds\windows\vc2013\bin\WindowsStore\Release\*.pdb WindowsStore\
