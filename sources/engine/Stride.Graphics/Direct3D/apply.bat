@echo off
for %%f in (*.cs) do (
    echo %%f
    copy %%f temp.txt
    echo #if %1 > %%f
    copy %%f+temp.txt %%f
    echo. >> %%f
    echo #endif >> %%f
    del temp.txt
)