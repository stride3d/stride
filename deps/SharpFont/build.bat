msbuild ..\..\externals\SharpFont\Source\SharpFont.sln /p:Configuration=Release

xcopy /Y /S ..\..\externals\SharpFont\Binaries\SharpFont\Portable\Release\* Portable\
xcopy /Y /S ..\..\externals\SharpFont\Binaries\SharpFont\iOS\Release\* iOS\
xcopy /Y /S ..\..\externals\SharpFont\Binaries\SharpFont\Linux\Release\* Linux\
