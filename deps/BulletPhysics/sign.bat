call "%ProgramFiles(x86)%\Microsoft Visual Studio 12.0\vc\vcvarsall.bat" x86

ildasm /all /out:Windows\BulletSharp.NetStandard.il BulletSharp.NetStandard.dll
ildasm /all /out:iOS\BulletSharp.NetStandard.il iOS\BulletSharp.NetStandard.dll

@echo "Please patch Windows\BulletSharp.NetStandard.il and iOS\BulletSharp.NetStandard.il Xenko.Core.Mathematics reference with .publickeytoken = ( BA CA CC 89 C3 B6 D5 56 )"
pause

mkdir Windows\Signed
mkdir iOS\Signed
ilasm /dll /key:..\..\build\paradox.snk /output:Windows\Signed\BulletSharp.NetStandard.dll Windows\BulletSharp.NetStandard.il
ilasm /dll /key:..\..\build\paradox.snk /output:iOS\Signed\BulletSharp.NetStandard.dll iOS\BulletSharp.NetStandard.il