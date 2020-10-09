
=======

Welcome to the 3030Engine source code repository!

3030Engine is a game engine built off of the stride game engine, mostly in c#. I am not accepting pull requests. feel free to use this for your projects.

_You will still se stride everywhere to keep everything Stable and working._


To learn more about Stride, visit [stride3d.net](https://stride3d.net/).

## License

Stride is covered by the [MIT License](LICENSE.md) unless stated otherwise (i.e. for some files that are copied from other projects).

You can find the list of third party projects [here](THIRD%20PARTY.md).

## Documentation

Find explanations and information about Stride:
* [Stride Manual](https://doc.stride3d.net/latest/manual/index.html)
* [API Reference](https://doc.stride3d.net/latest/api/index.html)
* [Release Notes](https://doc.stride3d.net/latest/ReleaseNotes/index.html)

## Community

Ask for help or report issues:
* [Chat with the community on Discord](https://discord.gg/WTBJme)


## Building from source

### Prerequisites

1. **Latest** [Git](https://git-scm.com/downloads) **with Large File Support** selected in the setup on the components dialog.
2. [Visual Studio 2019](https://www.visualstudio.com/downloads/) with the following workloads:
  * `.NET desktop development` with `.NET Framework 4.7.2 targeting pack`
  * `Desktop development with C++` with
    * `Windows 10 SDK (10.0.18362.0)` (it's currently enabled by default but it might change)
    * `MSVC v142 - VS2019 C++ x64/x86 build tools (v14.26)` or later version (should be enabled by default)
    * `C++/CLI support for v142 build tools (v14.26)` or later version **(not enabled by default)**
  * `.NET Core cross-platform development`
  * Optional (to target UWP): `Universal Windows Platform development` with
    * `Windows 10 SDK (10.0.18362.0)` or later version
    * `MSVC v142 - VS2019 C++ ARM build tools (v14.26)` or later version **(not enabled by default)**
  * Optional (to target iOS/Android): `Mobile development with .NET` and `Android SDK setup (API level 27)` individual component, then in Visual Studio go to `Tools > Android > Android SDK Manager` and install `NDK` (version 19+) from `Tools` tab.
3. **[FBX SDK 2019.0 VS2015](https://www.autodesk.com/developer-network/platform-technologies/fbx-sdk-2019-0)**

### Build Stride

1. Open a command prompt, point it to a directory and clone 3030Engine to it: `git clone https://github.com/wholesomeisland/3030Engine.git`
2. Open `<StrideDir>\build\Stride.sln` with Visual Studio 2019 and build `Stride.GameStudio` (it should be the default startup project) or run it from VS's toolbar.
* Optionally, open and build `Stride.Android.sln`, `Stride.iOS.sln`, etc.

If building failed:
* If you skipped one of the `Prerequisites` thinking that you already have the latest version, update to the latest anyway just to be sure.
* Visual Studio might have issues properly building if an outdated version of 2017 is present alongside 2019. If you want to keep VS 2017 make sure that it is up to date and that you are building Stride through VS 2019.
* Some changes might require a system reboot, try that if you haven't yet.

Do note that test solutions might fail but it should not prevent you from building `Stride.GameStudio`.

### Contribution Guidelines

Please check our [Contributing Guidelines](docs/CONTRIBUTING.md).

### Build Status

|Branch| **master** |
|:--:|:--:|
![.NET Core](https://github.com/3030Developers/3030Engine/workflows/.NET%20Core/badge.svg)
