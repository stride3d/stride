call "%ProgramFiles(x86)%\Microsoft Visual Studio 14.0\vc\vcvarsall.bat" x86
msbuild ..\..\externals\il-repack\cecil\Mono.Cecil.sln /Property:Configuration=net_4_0_Release;Platform="Any CPU"
copy ..\..\externals\il-repack\cecil\bin\net_4_0_Release\*.* .
