@echo OFF
setlocal
set HOME=%USERPROFILE%
"C:\Program Files\TortoiseSVN\bin\TortoiseProc.exe" /command:checkout /blockpathadjustments /path:../../externals/ExtendedWPFToolkit /url:https://wpftoolkit.svn.codeplex.com/svn
if NOT ERRORLEVEL 0 pause

