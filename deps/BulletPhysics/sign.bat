call "%ProgramFiles(x86)%\Microsoft Visual Studio 12.0\vc\vcvarsall.bat" x86

ildasm /all /out:Windows\BulletSharp.il Windows\BulletSharp.dll
ildasm /all /out:iOS\BulletSharp.il iOS\BulletSharp.dll

@echo "Please patch Windows\BulletSharp.il and iOS\BulletSharp.il Xenko.Core.Mathematics reference with .publickeytoken = ( BA CA CC 89 C3 B6 D5 56 )"
pause

mkdir Windows\Signed
mkdir iOS\Signed
ilasm /dll /key:..\..\build\paradox.snk /output:Windows\Signed\BulletSharp.dll Windows\BulletSharp.il
ilasm /dll /key:..\..\build\paradox.snk /output:iOS\Signed\BulletSharp.dll iOS\BulletSharp.il