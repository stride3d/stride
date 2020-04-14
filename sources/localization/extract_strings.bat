@set SOURCES_DIR=%~dp0..
@set MSGMERGE=%~dp0..\..\deps\gettext\msgmerge
@set OUTPUT_DIR=%SOURCES_DIR%\localization
@set TOOL_DIR=%SOURCES_DIR%\tools\Stride.Core.Translation.Extractor\bin\Debug\net472

@cd %OUTPUT_DIR%

rem Stride.Core.Presentation.pot
%TOOL_DIR%\Stride.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\presentation\Stride.Core.Presentation --domain-name=Stride.Core.Presentation --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs

rem Stride.Assets.Presentation.pot
%TOOL_DIR%\Stride.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\editor\Stride.Assets.Presentation --domain-name=Stride.Assets.Presentation --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs

rem Stride.Core.Assets.Editor.pot
%TOOL_DIR%\Stride.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\editor\Stride.Core.Assets.Editor --domain-name=Stride.Core.Assets.Editor --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs

rem Stride.GameStudio.pot
%TOOL_DIR%\Stride.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\editor\Stride.GameStudio --domain-name=Stride.GameStudio --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs

rem Update po files
FOR %%B IN (Stride.Core.Presentation Stride.Assets.Presentation Stride.Core.Assets.Editor Stride.GameStudio) DO (
  FOR %%A IN (ja fr es de ru zh_HANS-CN it ko) DO (
    %MSGMERGE% -U %%A\%%B.%%A.po %%B.pot
  )
)
