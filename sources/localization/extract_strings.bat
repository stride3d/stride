@set SOURCES_DIR=%~dp0..
@set MSGMERGE=%~dp0..\..\deps\gettext\msgmerge
@set OUTPUT_DIR=%SOURCES_DIR%\localization
@set TOOL_DIR=%SOURCES_DIR%\tools\Stride.Core.Translation.Extractor\bin\Debug\net10.0

@cd %OUTPUT_DIR%

rem Stride.Core.Presentation.pot
%TOOL_DIR%\Stride.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\presentation\Stride.Core.Presentation --domain-name=Stride.Core.Presentation --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs

rem Stride.Core.Presentation.Wpf.pot
%TOOL_DIR%\Stride.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\presentation\Stride.Core.Presentation.Wpf --domain-name=Stride.Core.Presentation.Wpf --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs

rem Stride.Assets.Editor.Wpf.pot
%TOOL_DIR%\Stride.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\editor\Stride.Assets.Editor.Wpf --domain-name=Stride.Assets.Editor.Wpf --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs

rem Stride.Core.Assets.Editor.Wpf.pot
%TOOL_DIR%\Stride.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\editor\Stride.Core.Assets.Editor.Wpf --domain-name=Stride.Core.Assets.Editor.Wpf --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs

rem Stride.Core.Assets.Presentation.pot
%TOOL_DIR%\Stride.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\editor\Stride.Core.Assets.Presentation --domain-name=Stride.Core.Assets.Presentation --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs

rem Stride.Core.Assets.Editor.pot
%TOOL_DIR%\Stride.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\editor\Stride.Core.Assets.Editor --domain-name=Stride.Core.Assets.Editor --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs

rem Stride.Assets.Presentation.pot
%TOOL_DIR%\Stride.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\editor\Stride.Assets.Presentation --domain-name=Stride.Assets.Presentation --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs

rem Stride.Assets.Editor.pot
%TOOL_DIR%\Stride.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\editor\Stride.Assets.Editor --domain-name=Stride.Assets.Editor --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs

rem Stride.Editor.pot
%TOOL_DIR%\Stride.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\editor\Stride.Editor --domain-name=Stride.Editor --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs

rem Stride.UI.Editor.pot
%TOOL_DIR%\Stride.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\plugins\UI\Stride.UI.Editor --domain-name=Stride.UI.Editor --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs

rem Stride.GameStudio.pot
%TOOL_DIR%\Stride.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\editor\Stride.GameStudio --domain-name=Stride.GameStudio --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs

rem Update po files
FOR %%B IN (Stride.Core.Presentation Stride.Core.Presentation.Wpf Stride.Assets.Editor.Wpf Stride.Core.Assets.Editor.Wpf Stride.Core.Assets.Presentation Stride.Core.Assets.Editor Stride.Assets.Presentation Stride.Assets.Editor Stride.Editor Stride.UI.Editor Stride.GameStudio) DO (
  FOR %%A IN (de es fr he_IL it ja ko mk nb_NO pl pt pt_BR ru zh_HANS-CN zh_Hant) DO (
    %MSGMERGE% -U %%A\%%B.%%A.po %%B.pot
  )
)
