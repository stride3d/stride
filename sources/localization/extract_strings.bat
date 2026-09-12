@set SOURCES_DIR=%~dp0..
@set MSGMERGE=%~dp0..\..\deps\gettext\msgmerge
@set OUTPUT_DIR=%SOURCES_DIR%\localization
@set TOOL_DIR=%SOURCES_DIR%\tools\Stride.Core.Translation.Extractor\bin\Debug\net10.0-windows

@cd %OUTPUT_DIR%

rem Stride.Core.Presentation.pot
%TOOL_DIR%\Stride.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\presentation\Stride.Core.Presentation --domain-name=Stride.Core.Presentation --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs

rem Stride.Core.Presentation.Wpf.pot
%TOOL_DIR%\Stride.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\presentation\Stride.Core.Presentation.Wpf --domain-name=Stride.Core.Presentation.Wpf --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs

rem Stride.Assets.Presentation.Wpf.pot
%TOOL_DIR%\Stride.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\editor\Stride.Assets.Presentation --domain-name=Stride.Assets.Presentation.Wpf --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs

rem Stride.Core.Assets.Editor.Wpf.pot
%TOOL_DIR%\Stride.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\editor\Stride.Core.Assets.Editor.Wpf --domain-name=Stride.Core.Assets.Editor.Wpf --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs

rem Stride.GameStudio.pot
%TOOL_DIR%\Stride.Core.Translation.Extractor.exe --directory=%SOURCES_DIR%\editor\Stride.GameStudio --domain-name=Stride.GameStudio --recursive --preserve-comments --exclude=*.Designer.cs --verbose *.xaml *.cs

rem Update po files
FOR %%B IN (Stride.Core.Presentation Stride.Core.Presentation.Wpf Stride.Assets.Presentation.Wpf Stride.Core.Assets.Editor.Wpf Stride.GameStudio) DO (
  FOR %%A IN (de es fr he_IL it ja ko mk nb_NO pl pt pt_BR ru zh_HANS-CN zh_Hant) DO (
    %MSGMERGE% -U %%A\%%B.%%A.po %%B.pot
  )
)
