call "%ProgramFiles(x86)%\Microsoft Visual Studio 12.0\vc\vcvarsall.bat" x86

ildasm /all /out:Windows\Debug\SharpFont.il Windows\Debug\SharpFont.dll
ilasm /dll /debug /key:..\..\build\paradox.snk  Windows\Debug\SharpFont.il

ildasm /all /out:Windows\Release\SharpFont.il Windows\Release\SharpFont.dll
ilasm /dll /pdb /key:..\..\build\paradox.snk Windows\Release\SharpFont.il

ildasm /all /out:Android\Debug\SharpFont.il Android\Debug\SharpFont.dll
ilasm /dll /debug /key:..\..\build\paradox.snk  Android\Debug\SharpFont.il

ildasm /all /out:Android\Release\SharpFont.il Android\Release\SharpFont.dll
ilasm /dll /pdb /key:..\..\build\paradox.snk Android\Release\SharpFont.il

ildasm /all /out:iOS\Debug\SharpFont.il iOS\Debug\SharpFont.dll
ilasm /dll /debug /key:..\..\build\paradox.snk iOS\Debug\SharpFont.il

ildasm /all /out:iOS\Release\SharpFont.il iOS\Release\SharpFont.dll
ilasm /dll /pdb /key:..\..\build\paradox.snk iOS\Release\SharpFont.il

del /s .\*.il
del /s .\*.res