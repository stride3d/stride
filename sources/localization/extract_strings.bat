@set SOURCES_DIR="%~dp0\.."
@set OUTPUT_DIR=%SOURCES_DIR%\localization\

@cd "%OUTPUT_DIR%"

rem Xenko.Core.Presentation.pot
..\..\Bin\Windows\Xenko.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\presentation\Xenko.Core.Presentation --domain-name=Xenko.Core.Presentation --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs

rem Xenko.Assets.Presentation.pot
..\..\Bin\Windows\Xenko.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\editor\Xenko.Assets.Presentation --domain-name=Xenko.Assets.Presentation --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs

rem Xenko.Core.Assets.Editor.pot
 ..\..\Bin\Windows\Xenko.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\editor\Xenko.Core.Assets.Editor --domain-name=Xenko.Core.Assets.Editor --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs

rem Xenko.GameStudio.pot
 ..\..\Bin\Windows\Xenko.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\editor\Xenko.GameStudio --domain-name=Xenko.GameStudio --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs
