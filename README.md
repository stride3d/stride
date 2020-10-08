
=======

Welcome to the 3030Engine source code repository!

3030Engine is a game engine built off of the stride game engine, mostly in c#. I am not accepting pull requests. feel free to use this for your projects.



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
This is CI results from the stride repo and may not reflect this one.
|Branch| **master** |
|:--:|:--:|
|Windows D3D11|<a href="https://teamcity.stride3d.net/viewType.html?buildTypeId=Engine_BuildWindowsD3d11&branch=master&guest=1"><img src="https://teamcity.stride3d.net/app/rest/builds/buildType:(id:Engine_BuildWindowsD3d11),branch:master/statusIcon"/></a>
|Windows D3D12|<a href="https://teamcity.stride3d.net/viewType.html?buildTypeId=Engine_BuildWindowsD3d12&branch=master&guest=1"><img src="https://teamcity.stride3d.net/app/rest/builds/buildType:(id:Engine_BuildWindowsD3d12),branch:master/statusIcon"/></a>
|Windows Vulkan|<a href="https://teamcity.stride3d.net/viewType.html?buildTypeId=Engine_BuildWindowsVulkan&branch=master&guest=1"><img src="https://teamcity.stride3d.net/app/rest/builds/buildType:(id:Engine_BuildWindowsVulkan),branch:master/statusIcon"/></a>
|Windows OpenGL|<a href="https://teamcity.stride3d.net/viewType.html?buildTypeId=Engine_BuildWindowsOpenGL&branch=master&guest=1"><img src="https://teamcity.stride3d.net/app/rest/builds/buildType:(id:Engine_BuildWindowsOpenGL),branch:master/statusIcon"/></a>
|Windows OpenGL ES|<a href="https://teamcity.stride3d.net/viewType.html?buildTypeId=Engine_BuildWindowsOpenGLES&branch=master&guest=1"><img src="https://teamcity.stride3d.net/app/rest/builds/buildType:(id:Engine_BuildWindowsOpenGLES),branch:master/statusIcon"/></a>
|UWP|<a href="https://teamcity.stride3d.net/viewType.html?buildTypeId=Engine_BuildWindowsUWP&branch=master&guest=1"><img src="https://teamcity.stride3d.net/app/rest/builds/buildType:(id:Engine_BuildWindowsUWP),branch:master/statusIcon"/></a>
|iOS|<a href="https://teamcity.stride3d.net/viewType.html?buildTypeId=Engine_BuildiOS&branch=master&guest=1"><img src="https://teamcity.stride3d.net/app/rest/builds/buildType:(id:Engine_BuildiOS),branch:master/statusIcon"/></a>
|Android|<a href="https://teamcity.stride3d.net/viewType.html?buildTypeId=Engine_BuildAndroid&branch=master&guest=1"><img src="https://teamcity.stride3d.net/app/rest/builds/buildType:(id:Engine_BuildAndroid),branch:master/statusIcon"/></a>
|Linux Vulkan|<a href="https://teamcity.stride3d.net/viewType.html?buildTypeId=Engine_BuildLinuxVulkan&branch=master&guest=1"><img src="https://teamcity.stride3d.net/app/rest/builds/buildType:(id:Engine_BuildLinuxVulkan),branch:master/statusIcon"/></a>
|Linux OpenGL|<a href="https://teamcity.stride3d.net/viewType.html?buildTypeId=Engine_BuildLinuxOpenGL&branch=master&guest=1"><img src="https://teamcity.stride3d.net/app/rest/builds/buildType:(id:Engine_BuildLinuxOpenGL),branch:master/statusIcon"/></a>
|Tests Windows Simple| <a href="https://teamcity.stride3d.net/viewType.html?buildTypeId=Engine_Tests_WindowsSimple&branch=master&guest=1"><img src="https://teamcity.stride3d.net/app/rest/builds/buildType:(id:Engine_Tests_WindowsSimple),branch:master/statusIcon"/></a>
|Tests Windows D3D11|<a href="https://teamcity.stride3d.net/viewType.html?buildTypeId=Engine_Tests_WindowsD3D11&branch=master&guest=1"><img src="https://teamcity.stride3d.net/app/rest/builds/buildType:(id:Engine_Tests_WindowsD3D11),branch:master/statusIcon"/></a> 
