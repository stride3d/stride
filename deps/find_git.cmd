:: Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
:: See LICENSE.md for full license information.
@echo OFF
where /q git
IF ERRORLEVEL 0 (
  @set GIT_CMD=git
) ELSE IF EXIST "%ProgramFiles%\Git\Bin\git.exe" (
  @set GIT_CMD="%ProgramFiles%\Git\Bin\git.exe"
) ELSE IF EXIST "%ProgramFiles(x86)%\Git\Bin\git.exe" (
  @set GIT_CMD="%ProgramFiles(x86)%\Git\Bin\git.exe"
) ELSE EXIT /B 1
