![Xenko](https://xenko.com/images/external/xenko-logo-side.png)
=======

Welcome to the Xenko source code repository!

Xenko is an open-source C# game engine for realistic rendering and VR. 
The engine is highly modular and aims at giving game makers more flexibility in their development.
Xenko comes with an editor that allows you create and manage the content of your games or applications in a visual and intuitive way.

![Xenko Editor](https://xenko.com/images/external/script-editor.png)

To learn more about Xenko, visit [xenko.com](https://xenko.com/).

## License

Xenko is covered by [MIT](LICENSE.md), unless stated otherwise (i.e. for some files that are copied from other projects).

You can find the list of third party projects [here](THIRD%20PARTY.md).

Contributors need to sign the following [Contribution License Agreement](docs/ContributorLicenseAgreement.md).

## Documentation

Find explanations and information about Xenko:
* [Xenko Manual](https://doc.xenko.com/latest/manual/index.html)
* [API Reference](https://doc.xenko.com/latest/api/index.html)
* [Release Notes](https://doc.xenko.com/latest/ReleaseNotes/index.html)

## Community

Ask for help or report issues:
* [Chat with the community on Discord](https://discord.gg/f6aerfE) [![Join the chat at https://discord.gg/f6aerfE](https://img.shields.io/discord/500285081265635328.svg?style=flat&logo=discord&label=discord)](https://discord.gg/f6aerfE)
* [Discuss topics on our forums](http://forums.xenko.com/)
* [Report engine issues](https://github.com/xenko3d/xenko/issues)
* [Donate to support the project](https://www.patreon.com/xenko)

## Building from source

### Prerequisites

1. [Git](https://git-scm.com/downloads) (recent version that includes LFS, or install [Git LFS](https://git-lfs.github.com/) separately).
2. [Visual Studio 2017](https://www.visualstudio.com/downloads/) with the following workloads:
  * `.NET desktop development` with `.NET Framework 4.7.2 targeting pack`
  * `Desktop development with C++` with `Windows 10 SDK (10.0.16299)` or later and `VC++ 2017 version 15.9 v14.16 latest v141 tools` or later (both should be enabled by default)
  * `.NET Core cross-platform development`
  * Optional (to target UWP): `Universal Windows Platform development` with `Windows 10 SDK (10.0.16299.0)` and `Windows 10 SDK (10.0.17134.0)`
  * Optional (to target iOS/Android): `Mobile development with .NET` and `Android NDK R13B+` individual component
3. [FBX SDK 2019.0 VS2015](https://www.autodesk.com/developer-network/platform-technologies/fbx-sdk-2019-0)

### Build Xenko

1. Clone Xenko: `git clone https://github.com/xenko3d/xenko.git`
2. Open `<XenkoDir>\build\Xenko.sln` with Visual Studio 2017, and build.
3. Open `<XenkoDir>\samples\XenkoSamples.sln` and play with the samples.
4. Optionally, open and build `Xenko.Android.sln`, `Xenko.iOS.sln`, etc...

### Contribution Guidelines

Please check our [Contributing Guidelines](docs/CONTRIBUTING.md).

### Build Status

|Branch| **master** |
|:--:|:--:|
|Windows D3D11|<a href="https://teamcity.xenko.com/viewType.html?buildTypeId=Engine_BuildWindowsD3d11&branch=master&guest=1"><img src="https://teamcity.xenko.com/app/rest/builds/buildType:(id:Engine_BuildWindowsD3d11),branch:master/statusIcon"/></a>
|Windows D3D12|<a href="https://teamcity.xenko.com/viewType.html?buildTypeId=Engine_BuildWindowsD3d12&branch=master&guest=1"><img src="https://teamcity.xenko.com/app/rest/builds/buildType:(id:Engine_BuildWindowsD3d12),branch:master/statusIcon"/></a>
|Windows Vulkan|<a href="https://teamcity.xenko.com/viewType.html?buildTypeId=Engine_BuildWindowsVulkan&branch=master&guest=1"><img src="https://teamcity.xenko.com/app/rest/builds/buildType:(id:Engine_BuildWindowsVulkan),branch:master/statusIcon"/></a>
|Windows OpenGL|<a href="https://teamcity.xenko.com/viewType.html?buildTypeId=Engine_BuildWindowsOpenGL&branch=master&guest=1"><img src="https://teamcity.xenko.com/app/rest/builds/buildType:(id:Engine_BuildWindowsOpenGL),branch:master/statusIcon"/></a>
|Windows OpenGL ES|<a href="https://teamcity.xenko.com/viewType.html?buildTypeId=Engine_BuildWindowsOpenGLES&branch=master&guest=1"><img src="https://teamcity.xenko.com/app/rest/builds/buildType:(id:Engine_BuildWindowsOpenGLES),branch:master/statusIcon"/></a>
|UWP|<a href="https://teamcity.xenko.com/viewType.html?buildTypeId=Engine_BuildWindowsUWP&branch=master&guest=1"><img src="https://teamcity.xenko.com/app/rest/builds/buildType:(id:Engine_BuildWindowsUWP),branch:master/statusIcon"/></a>
|iOS|<a href="https://teamcity.xenko.com/viewType.html?buildTypeId=Engine_BuildiOS&branch=master&guest=1"><img src="https://teamcity.xenko.com/app/rest/builds/buildType:(id:Engine_BuildiOS),branch:master/statusIcon"/></a>
|Android|<a href="https://teamcity.xenko.com/viewType.html?buildTypeId=Engine_BuildWindowsAndroid&branch=master&guest=1"><img src="https://teamcity.xenko.com/app/rest/builds/buildType:(id:Engine_BuildAndroid),branch:master/statusIcon"/></a>
|Linux Vulkan|<a href="https://teamcity.xenko.com/viewType.html?buildTypeId=Engine_BuildLinuxVulkan&branch=master&guest=1"><img src="https://teamcity.xenko.com/app/rest/builds/buildType:(id:Engine_BuildLinuxVulkan),branch:master/statusIcon"/></a>
|Linux OpenGL|<a href="https://teamcity.xenko.com/viewType.html?buildTypeId=Engine_BuildLinuxOpenGL&branch=master&guest=1"><img src="https://teamcity.xenko.com/app/rest/builds/buildType:(id:Engine_BuildLinuxOpenGL),branch:master/statusIcon"/></a>
|Tests Windows Simple| <a href="https://teamcity.xenko.com/viewType.html?buildTypeId=Engine_Tests_WindowsSimple&branch=master&guest=1"><img src="https://teamcity.xenko.com/app/rest/builds/buildType:(id:Engine_Tests_WindowsSimple),branch:master/statusIcon"/></a>
|Tests Windows D3D11|<a href="https://teamcity.xenko.com/viewType.html?buildTypeId=Engine_Tests_WindowsD3D11&branch=master&guest=1"><img src="https://teamcity.xenko.com/app/rest/builds/buildType:(id:Engine_Tests_WindowsD3D11),branch:master/statusIcon"/></a> 
