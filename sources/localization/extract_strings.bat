@set SOURCES_DIR=%~dp0..
@set MSGMERGE=%~dp0..\..\deps\gettext\msgmerge
@set OUTPUT_DIR=%SOURCES_DIR%\localization
@set TOOL_DIR=%SOURCES_DIR%\tools\Xenko.Core.Translation.Extractor\bin\Debug\net472

@cd %OUTPUT_DIR%

rem Xenko.Core.Presentation.pot
%TOOL_DIR%\Xenko.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\presentation\Xenko.Core.Presentation --domain-name=Xenko.Core.Presentation --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs

rem Xenko.Assets.Presentation.pot
%TOOL_DIR%\Xenko.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\editor\Xenko.Assets.Presentation --domain-name=Xenko.Assets.Presentation --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs

rem Xenko.Core.Assets.Editor.pot
%TOOL_DIR%\Xenko.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\editor\Xenko.Core.Assets.Editor --domain-name=Xenko.Core.Assets.Editor --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs

rem Xenko.GameStudio.pot
%TOOL_DIR%\Xenko.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\editor\Xenko.GameStudio --domain-name=Xenko.GameStudio --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs

rem Update po files
FOR %%B IN (Xenko.Core.Presentation Xenko.Assets.Presentation Xenko.Core.Assets.Editor Xenko.GameStudio) DO (
  FOR %%A IN (ja fr es de ru zh_HANS-CN) DO (
    %MSGMERGE% -U %%A\%%B.%%A.po %%B.pot
  )
)
