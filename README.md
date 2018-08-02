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

You can find the list of third party projects [THIRD PARTY.md](THIRD%20PARTY.md).

Contributors need to sign the following [Contribution License Agreement](doc/ContributorLicenseAgreement.md).

## Documentation

Find explanations and information about Xenko:
* [Xenko Manual](https://doc.xenko.com/latest/manual/index.html)
* [API Reference](https://doc.xenko.com/latest/api/Xenko.Core.Assets.html)
* [Release Notes](https://doc.xenko.com/latest/ReleaseNotes/index.html)

## Community

Ask for help or report issues:
* [Chat with the community](https://gitter.im/xenko3d/xenko?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
* [Ask and answer questions](http://answers.xenko.com/)
* [Discuss topics on our forums](http://forums.xenko.com/)
* [Report engine issues](https://github.com/xenko3d/xenko/issues)

## Building from source

### Prerequisites

1. [Git](https://git-scm.com/downloads) (recent version that includes LFS, or install [Git LFS](https://git-lfs.github.com/) separately).
2. [Visual Studio 2017](https://www.visualstudio.com/downloads/) with the following workloads:
  * .NET desktop development (with .NET Framework 4.6.2 development tools)
  * Desktop development with C++
  * Optional: Universal Windows Platform development
  * Optional: Mobile development with .NET
3. [FBX SDK 2019.0 VS2015](https://www.autodesk.com/developer-network/platform-technologies/fbx-sdk-2019-0)

### Build Xenko

1. Clone Xenko: `git clone https://github.com/xenko3d/xenko.git`
2. Set *XenkoDir* environment variable to point to your `<XenkoDir>`
3. Open `<XenkoDir>\build\Xenko.sln` with Visual Studio 2017, and build.
4. Open `<XenkoDir>\samples\XenkoSamples.sln` and play with the samples.
5. Optionally, open and build `Xenko.Android.sln`, `Xenko.iOS.sln`, etc...
