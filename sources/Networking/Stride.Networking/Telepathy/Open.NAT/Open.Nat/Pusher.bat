@echo off
cd %~dp0

set /p version= Version? 

echo Packing
..\.nuget\nuget pack Open.NAT.nuspec -version %version%
echo Pushing to nuget.org
set /p apikey= API Key? 
..\.nuget\nuget push Open.NAT.%version%.nupkg -s https://nuget.org/ -ApiKey %apikey%
echo Done!
pause
