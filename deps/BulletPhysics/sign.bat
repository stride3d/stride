call "%ProgramFiles(x86)%\Microsoft Visual Studio 12.0\vc\vcvarsall.bat" x86

ildasm /all /out:BulletSharp.NetStandard.il BulletSharp.NetStandard.dll
ildasm /all /out:iOS\BulletSharp.NetStandard.il iOS\BulletSharp.NetStandard.dll

@echo "Please patch BulletSharp.NetStandard.il and iOS\BulletSharp.NetStandard.il Xenko.Core.Mathematics reference with .publickeytoken = ( BA CA CC 89 C3 B6 D5 56 )"
pause

mkdir Signed
mkdir iOS\Signed
ilasm /dll /key:..\..\build\xenko.public.snk /output:Signed\BulletSharp.NetStandard.dll BulletSharp.NetStandard.il
ilasm /dll /key:..\..\build\xenko.public.snk /output:iOS\Signed\BulletSharp.NetStandard.dll iOS\BulletSharp.NetStandard.il