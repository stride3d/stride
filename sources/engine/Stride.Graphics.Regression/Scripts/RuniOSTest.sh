#!/bin/bash

MDTOOL=/Applications/MonoDevelop.app/Contents/MacOS/mdtool
MTOUCH=/Developer/MonoTouch/usr/bin/mtouch

# build
$MDTOOL -v build -t:Build "-c:Release|iPhone" Stride.iOS.sln

# create app
$MTOUCH -dev="" MyApp.app -c <em>"???"</em> foo.exe

# install on device
$MTOUCH --devname=DEVICE -installdev=MyApp.app

# kill previous instance
$MTOUCH --devname=DEVICE -killdev com.xamarin.myapp

# run on device
mono --launchdev com.xamarin.myapp -autoexit -logfile=dev-results.log