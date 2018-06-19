#!/bin/sh

osName=`uname -s`
dotnet=`which dotnet`

case $osName in
    Linux)
        dotnet=`readlink -f $dotnet`
        is64=`file $dotnet | grep "64-bit"`

        ;;

    Darwin)
        is64=`file $dotnet | grep "x86_64"`
        ;;
esac

if [ -n "$is64" ]; then
    echo Copying 64-bit native libraries
    cp -f x64/* .
else
    echo Copying 32-bit native libraries
    cp -f x86/* .
fi
