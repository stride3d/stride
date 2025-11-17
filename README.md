<p>
<a href="https://www.stride3d.net/">
<picture>
      <source media="(prefers-color-scheme: dark)" srcset="https://media.githubusercontent.com/media/stride3d/stride/84092e8aa924e2039b3f8d968907b48fc699c6b3/sources/data/images/Logo/stride-logo-readme-white.png">
      <source media="(prefers-color-scheme: light)" srcset="https://media.githubusercontent.com/media/stride3d/stride/84092e8aa924e2039b3f8d968907b48fc699c6b3/sources/data/images/Logo/stride-logo-readme-black.png">
      <img alt="The stride logo, a geometrical 'S' in the form of a cube" src="https://media.githubusercontent.com/media/stride3d/stride/84092e8aa924e2039b3f8d968907b48fc699c6b3/sources/data/images/Logo/stride-logo-readme-black.png">
</picture>
</a>
</p>

[![Join the chat at https://discord.gg/f6aerfE](https://img.shields.io/discord/500285081265635328.svg?style=flat&logo=discord&label=discord&logoColor=f2f2f2)](https://discord.gg/f6aerfE)
[![All Contributors](https://img.shields.io/github/all-contributors/stride3d/stride?color=ee8449)](#contributors)
[![Financial sponsors](https://img.shields.io/opencollective/all/stride3d?logo=opencollective)](https://opencollective.com/stride3d)
[![License](https://img.shields.io/badge/license-MIT-blue)](https://github.com/stride3d/stride/blob/master/LICENSE.md)
[![Contributor Covenant](https://img.shields.io/badge/contributor%20covenant-2.0-4baaaa.svg)](CODE_OF_CONDUCT.md)

# Welcome to the Stride Game Engine

Stride is an open-source C# game engine designed for realistic rendering and VR. Highly modular, it aims to give game makers more flexibility in their development. Stride comes with an editor, [Game Studio](https://doc.stride3d.net/latest/en/manual/game-studio/index.html), which allows you to create and manage the content of your games or applications visually and intuitively. To learn more about Stride, visit [stride3d.net](https://stride3d.net/).

![Stride Editor](https://stride3d.net/images/external/script-editor.png)

This README is intended for users who want to build the Stride engine from source or contribute to its development. If your goal is to create games using Stride, we recommend visiting the [Get started with Stride](https://doc.stride3d.net/latest/en/manual/get-started/index.html) guide. There, you'll find detailed instructions on downloading, installing, and getting started with game development in Stride.

## 🤝 Contributing

Want to get involved? See our [Contributing section](.github/CONTRIBUTING.md) for how to ask questions, report bugs, submit pull requests (including good first issues), and how you can earn money by contributing via funded tasks/bug bounties.

## 🗺️ Roadmap

Our [Roadmap](https://doc.stride3d.net/latest/en/contributors/roadmap.html) communicates upcoming changes to the Stride engine.

## 🛠️ Building from Source

### Prerequisites

1. **Latest [Git](https://git-scm.com/downloads)** with **Large File Support** selected during setup. For convenience, you might also use a Git UI client like [GitExtensions](https://gitextensions.github.io/).
2. **[.NET 10.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)**
   - Run `dotnet --info` in a console or PowerShell window to see which versions you have installed.
3. **[Visual Studio 2026](https://visualstudio.microsoft.com/downloads/)** (the Community edition is free), with the following workloads. Follow this link if you would rather use [a different IDE or the command line](#build-stride-without-visual-studio).
   - **.NET desktop development** with **.NET Framework 4.7.2 targeting pack** *(should be enabled by default)*
   - **Desktop development with C++** with:
     - **Windows 11 SDK (10.0.22621.0)** or a later version *(should be enabled by default)*
     - **MSVC v143 - VS 2022 C++ x64/x86 build tools (Latest)** *(should be enabled by default)*
     - **MSVC v143 - VS 2022 C++ ARM64/ARM64EC build tools (Latest)** *(not enabled by default, click Individual components tab to select or search)*
     - **C++/CLI support for v143 build tools (Latest)** *(not enabled by default)*
   - *Optional* (to target iOS/Android): **.NET Multi-platform App UI development** and the **Android SDK setup** individual component (enabled by default). Then, in Visual Studio, go to `Tools > Android > Android SDK Manager` and install **NDK** (version 20.1+) from the `Tools` tab.
   - *Optional* (to build the VSIX package): **Visual Studio extension development**

> [!NOTE]
> The installation of Visual Studio with the required components may require up to **19 GB of disk space**, depending on your system and selected components.

> [!WARNING]
> If this is your first time installing the .NET SDK, you might need to restart your system after the installation so that the system can recognize the new environment variables.

### Build Stride

1. **Clone the repository** using a Git UI client or from the command line:
   ```bash
   git lfs clone https://github.com/stride3d/stride.git
   ```
2. **Open the solution:**
   - Open `<StrideDir>\build\Stride.sln` with Visual Studio 2026. 
   - Build the `Stride.GameStudio` project in the `60-Editor` solution folder (it should be the default startup project) or run it directly from Visual Studio's toolbar.
   - _Optionally_, open and build `Stride.Android.sln`, `Stride.iOS.sln`, etc.

> [!WARNING]
> **Do NOT use GitHub -> Code -> Download ZIP** option, as this won't include the LFS files.

### Build Stride without Visual Studio

1. **Install** [Visual Studio Build Tools](https://visualstudio.microsoft.com/downloads/) (Go to *Tools for Visual Studio* and press download next to Build Tools for Visual Studio 2026) with the same prerequisites listed above.
2. **Add MSBuild to your system's PATH:**
   - Add MSBuild's directory to your `PATH` environment variable (e.g., `c:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\MSBuild\Current\Bin`).
3. **Clone the repository:**
   ```bash
   git lfs clone https://github.com/stride3d/stride.git

   ```
4. **Build using the command line:**
   - Navigate to the `/build` directory in the command prompt and run:
   ```bash
   msbuild /t:Restore Stride.sln

   ```
   - Then run:
   ```bash
   compile.bat

   ```

### If Building Fails

* Some errors for test projects are normal, GameStudio will start anyway.
* The Visual Studio extension might fail to build if you are missing the [Visual Studio SDK](https://learn.microsoft.com/en-us/visualstudio/extensibility/installing-the-visual-studio-sdk?view=vs-2026), but Game Studio will start anyway.
* If you skipped any of the **Prerequisites** thinking you already have the latest version, please update to the latest to be sure.
* Visual Studio might have issues building properly if an older version is present alongside 2026. If you want to keep those versions, ensure they are up to date and that you are building Stride using Visual Studio 2026.
* Your system's `PATH` should not contain older versions of MSBuild (e.g., `...\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin` should be removed).
* Some changes might require a system reboot. Try that if you haven't yet, for example, if you see these errors:
  * `Could not find a compatible version of MSBuild.`
  * `Path to dotnet executable is not set.`
* Ensure that Git, Git LFS, and Visual Studio can access the internet.
* Close Visual Studio, clear the NuGet cache (`dotnet nuget locals all --clear`), delete the hidden `.vs` folder inside `\build` and the files inside `bin\packages`, kill any `msbuild` and other Visual Studio processes, then build the whole solution and run GameStudio.

> [!WARNING]
> Test solutions might fail, but this should not prevent you from building `Stride.GameStudio`.

### Contribution Guidelines

Please check our [Contributing Guidelines](https://doc.stride3d.net/latest/en/contributors/index.html).

## 🔬 Build Status

|Branch| **master** |
|:--:|:--:|
|Windows D3D11|<a href="https://teamcity.stride3d.net/viewType.html?buildTypeId=Engine_BuildWindowsD3d11&branch=master&guest=1"><img src="https://teamcity.stride3d.net/app/rest/builds/buildType:(id:Engine_BuildWindowsD3d11),branch:master/statusIcon"/></a>
|Windows D3D12|<a href="https://teamcity.stride3d.net/viewType.html?buildTypeId=Engine_BuildWindowsD3d12&branch=master&guest=1"><img src="https://teamcity.stride3d.net/app/rest/builds/buildType:(id:Engine_BuildWindowsD3d12),branch:master/statusIcon"/></a>
|Windows Vulkan|<a href="https://teamcity.stride3d.net/viewType.html?buildTypeId=Engine_BuildWindowsVulkan&branch=master&guest=1"><img src="https://teamcity.stride3d.net/app/rest/builds/buildType:(id:Engine_BuildWindowsVulkan),branch:master/statusIcon"/></a>
|Windows OpenGL|<a href="https://teamcity.stride3d.net/viewType.html?buildTypeId=Engine_BuildWindowsOpenGL&branch=master&guest=1"><img src="https://teamcity.stride3d.net/app/rest/builds/buildType:(id:Engine_BuildWindowsOpenGL),branch:master/statusIcon"/></a>
|Windows OpenGL ES|<a href="https://teamcity.stride3d.net/viewType.html?buildTypeId=Engine_BuildWindowsOpenGLES&branch=master&guest=1"><img src="https://teamcity.stride3d.net/app/rest/builds/buildType:(id:Engine_BuildWindowsOpenGLES),branch:master/statusIcon"/></a>
|iOS|<a href="https://teamcity.stride3d.net/viewType.html?buildTypeId=Engine_BuildiOS&branch=master&guest=1"><img src="https://teamcity.stride3d.net/app/rest/builds/buildType:(id:Engine_BuildiOS),branch:master/statusIcon"/></a>
|Android|<a href="https://teamcity.stride3d.net/viewType.html?buildTypeId=Engine_BuildAndroid&branch=master&guest=1"><img src="https://teamcity.stride3d.net/app/rest/builds/buildType:(id:Engine_BuildAndroid),branch:master/statusIcon"/></a>
|Linux Vulkan|<a href="https://teamcity.stride3d.net/viewType.html?buildTypeId=Engine_BuildLinuxVulkan&branch=master&guest=1"><img src="https://teamcity.stride3d.net/app/rest/builds/buildType:(id:Engine_BuildLinuxVulkan),branch:master/statusIcon"/></a>
|Linux OpenGL|<a href="https://teamcity.stride3d.net/viewType.html?buildTypeId=Engine_BuildLinuxOpenGL&branch=master&guest=1"><img src="https://teamcity.stride3d.net/app/rest/builds/buildType:(id:Engine_BuildLinuxOpenGL),branch:master/statusIcon"/></a>
|Tests Windows Simple| <a href="https://teamcity.stride3d.net/viewType.html?buildTypeId=Engine_Tests_WindowsSimple&branch=master&guest=1"><img src="https://teamcity.stride3d.net/app/rest/builds/buildType:(id:Engine_Tests_WindowsSimple),branch:master/statusIcon"/></a>
|Tests Windows D3D11|<a href="https://teamcity.stride3d.net/viewType.html?buildTypeId=Engine_Tests_WindowsD3D11&branch=master&guest=1"><img src="https://teamcity.stride3d.net/app/rest/builds/buildType:(id:Engine_Tests_WindowsD3D11),branch:master/statusIcon"/></a> 

## 📖 Stride Documentation Landscape

The Stride documentation is organized across different locations. Here's how it's structured:

1. [Stride Game Engine](https://github.com/stride3d/stride) - The main repository for Stride.
   - [Contributing to Stride](https://doc.stride3d.net/latest/en/contributors/engine/index.html) - A guide for developers interested in contributing to or developing the Stride engine.
1. [Stride Website](https://www.stride3d.net/) - The official site showcasing Stride, featuring:
   - [Features](https://www.stride3d.net/features/) 
   - [Blog](https://www.stride3d.net/blog/)
   - [Community](https://www.stride3d.net/community/)
   - [Download](https://www.stride3d.net/download/)
   - [Sponsor](https://www.stride3d.net/sponsor/)
   - [Contributing to the Website](https://doc.stride3d.net/latest/en/contributors/website/index.html) - Guide for contributing to the Stride website.
2. [Stride Docs](https://doc.stride3d.net/) - The official documentation, including:
   - [Manual](https://doc.stride3d.net/latest/en/manual/index.html)
   - [Tutorials](https://doc.stride3d.net/latest/en/tutorials/index.html)
   - [Release Notes](https://doc.stride3d.net/latest/en/ReleaseNotes/ReleaseNotes.html)
   - [Ways to contribute](https://doc.stride3d.net/latest/en/contributors/index.html)
   - [API Reference](https://doc.stride3d.net/latest/en/api/index.html)
   - [Community Resources](https://doc.stride3d.net/latest/en/community-resources/index.html) - Demos, articles, shaders, physics examples, and more.
   - [Contributing to the Docs](https://doc.stride3d.net/latest/en/contributors/documentation/index.html) - Guide for contributing to the Stride documentation.
4. [Stride Community Toolkit](https://stride3d.github.io/stride-community-toolkit/index.html) - A set of C# helpers and extensions to enhance your experience with the Stride game engine.
   - [Contributing to Toolkit](https://github.com/stride3d/stride-community-toolkit) - Contribute to or explore the toolkit's source code.

## 🌐 .NET Foundation

This project is supported by the [.NET Foundation](http://www.dotnetfoundation.org).

## 🛡️ License

Stride is covered by the [MIT License](LICENSE.md) unless stated otherwise (i.e. for some files that are copied from other projects). You can find the list of third-party projects [here](THIRD%20PARTY.md). Contributors need to sign the following [Contribution License Agreement](https://github.com/dotnet-foundation/.github/blob/main/CLA/dotnetfoundation.yml).

## ✨ Contributors 

Thanks to all these wonderful people who have contributed to Stride!

<!-- ALL-CONTRIBUTORS-LIST:START - Do not remove or modify this section -->
<!-- prettier-ignore-start -->
<!-- markdownlint-disable -->
<table>
  <tbody>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="https://stride3d.net"><img src="https://avatars.githubusercontent.com/u/527565?v=4?s=100" width="100px;" alt="xen2"/><br /><sub><b>xen2</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=xen2" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/Eideren"><img src="https://avatars.githubusercontent.com/u/5742236?v=4?s=100" width="100px;" alt="Eideren"/><br /><sub><b>Eideren</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=Eideren" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://www.aggror.com"><img src="https://avatars.githubusercontent.com/u/3499539?v=4?s=100" width="100px;" alt="Jorn Theunissen"/><br /><sub><b>Jorn Theunissen</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=Aggror" title="Documentation">📖</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/tebjan"><img src="https://avatars.githubusercontent.com/u/1094716?v=4?s=100" width="100px;" alt="Tebjan Halm"/><br /><sub><b>Tebjan Halm</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=tebjan" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/azeno"><img src="https://avatars.githubusercontent.com/u/573618?v=4?s=100" width="100px;" alt="Elias Holzer"/><br /><sub><b>Elias Holzer</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=azeno" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="http://www.tinyrocket.se"><img src="https://avatars.githubusercontent.com/u/204513?v=4?s=100" width="100px;" alt="Johan Gustafsson"/><br /><sub><b>Johan Gustafsson</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=johang88" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/ykafia"><img src="https://avatars.githubusercontent.com/u/32330908?v=4?s=100" width="100px;" alt="Youness KAFIA"/><br /><sub><b>Youness KAFIA</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=ykafia" title="Code">💻</a></td>
    </tr>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="http://md-techblog.net.pl"><img src="https://avatars.githubusercontent.com/u/10709060?v=4?s=100" width="100px;" alt="Marian Dziubiak"/><br /><sub><b>Marian Dziubiak</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=manio143" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/AmbulantRex"><img src="https://avatars.githubusercontent.com/u/21176662?v=4?s=100" width="100px;" alt="AmbulantRex"/><br /><sub><b>AmbulantRex</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=AmbulantRex" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/Basewq"><img src="https://avatars.githubusercontent.com/u/1356956?v=4?s=100" width="100px;" alt="Basewq"/><br /><sub><b>Basewq</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=Basewq" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/herocrab"><img src="https://avatars.githubusercontent.com/u/35175947?v=4?s=100" width="100px;" alt="Jarmo"/><br /><sub><b>Jarmo</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=herocrab" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://junakovic.com"><img src="https://avatars.githubusercontent.com/u/60072552?v=4?s=100" width="100px;" alt="Antonio Junaković"/><br /><sub><b>Antonio Junaković</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=cstdbool" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/Kryptos-FR"><img src="https://avatars.githubusercontent.com/u/3006525?v=4?s=100" width="100px;" alt="Nicolas Musset"/><br /><sub><b>Nicolas Musset</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=Kryptos-FR" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/jasonswearingen"><img src="https://avatars.githubusercontent.com/u/814134?v=4?s=100" width="100px;" alt="Novaleaf"/><br /><sub><b>Novaleaf</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=jasonswearingen" title="Code">💻</a></td>
    </tr>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/salahchafai"><img src="https://avatars.githubusercontent.com/u/64394387?v=4?s=100" width="100px;" alt="salahchafai"/><br /><sub><b>salahchafai</b></sub></a><br /><a href="#design-salahchafai" title="Design">🎨</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://digitaltheory.biz/"><img src="https://avatars.githubusercontent.com/u/397608?v=4?s=100" width="100px;" alt="Mehar"/><br /><sub><b>Mehar</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=MeharDT" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="http://www.VaclavElias.com"><img src="https://avatars.githubusercontent.com/u/4528464?v=4?s=100" width="100px;" alt="Vaclav Elias"/><br /><sub><b>Vaclav Elias</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=VaclavElias" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/EternalTamago"><img src="https://avatars.githubusercontent.com/u/13661631?v=4?s=100" width="100px;" alt="EternalTamago"/><br /><sub><b>EternalTamago</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=EternalTamago" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/WhyPenguins"><img src="https://avatars.githubusercontent.com/u/42032199?v=4?s=100" width="100px;" alt="WhyPenguins"/><br /><sub><b>WhyPenguins</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=WhyPenguins" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/aunpyz"><img src="https://avatars.githubusercontent.com/u/14342782?v=4?s=100" width="100px;" alt="Aunnop Kattiyanet"/><br /><sub><b>Aunnop Kattiyanet</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=aunpyz" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/makotech222"><img src="https://avatars.githubusercontent.com/u/4389156?v=4?s=100" width="100px;" alt="Anon"/><br /><sub><b>Anon</b></sub></a><br /><a href="#design-makotech222" title="Design">🎨</a></td>
    </tr>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/D3ZAX"><img src="https://avatars.githubusercontent.com/u/15343372?v=4?s=100" width="100px;" alt="D3ZAX"/><br /><sub><b>D3ZAX</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=D3ZAX" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/phr00t"><img src="https://avatars.githubusercontent.com/u/5983470?v=4?s=100" width="100px;" alt="Phr00t"/><br /><sub><b>Phr00t</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=phr00t" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="http://schnellebuntebilder.de/"><img src="https://avatars.githubusercontent.com/u/646501?v=4?s=100" width="100px;" alt="sebl"/><br /><sub><b>sebl</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=sebllll" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/Artromskiy"><img src="https://avatars.githubusercontent.com/u/47901401?v=4?s=100" width="100px;" alt="Artromskiy"/><br /><sub><b>Artromskiy</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=Artromskiy" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/TheKeyblader"><img src="https://avatars.githubusercontent.com/u/30444673?v=4?s=100" width="100px;" alt="Jean-François Pustay"/><br /><sub><b>Jean-François Pustay</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=TheKeyblader" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/Earthmark"><img src="https://avatars.githubusercontent.com/u/1251609?v=4?s=100" width="100px;" alt="Daniel Miller"/><br /><sub><b>Daniel Miller</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=Earthmark" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="http://joreg.ath.cx"><img src="https://avatars.githubusercontent.com/u/1067952?v=4?s=100" width="100px;" alt="joreg"/><br /><sub><b>joreg</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=joreg" title="Code">💻</a></td>
    </tr>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/jrinker03"><img src="https://avatars.githubusercontent.com/u/49572939?v=4?s=100" width="100px;" alt="James Rinker"/><br /><sub><b>James Rinker</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=jrinker03" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/tristanmcpherson"><img src="https://avatars.githubusercontent.com/u/979937?v=4?s=100" width="100px;" alt="Tristan McPherson"/><br /><sub><b>Tristan McPherson</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=tristanmcpherson" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/ericwj"><img src="https://avatars.githubusercontent.com/u/9473119?v=4?s=100" width="100px;" alt="Eric"/><br /><sub><b>Eric</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=ericwj" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/gregsn"><img src="https://avatars.githubusercontent.com/u/575557?v=4?s=100" width="100px;" alt="Sebastian Gregor"/><br /><sub><b>Sebastian Gregor</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=gregsn" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="http://insomnyawolf.github.io"><img src="https://avatars.githubusercontent.com/u/18150917?v=4?s=100" width="100px;" alt="insomnyawolf"/><br /><sub><b>insomnyawolf</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=insomnyawolf" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/Doprez"><img src="https://avatars.githubusercontent.com/u/73259914?v=4?s=100" width="100px;" alt="Doprez"/><br /><sub><b>Doprez</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=Doprez" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/Jklawreszuk"><img src="https://avatars.githubusercontent.com/u/31008367?v=4?s=100" width="100px;" alt="Jakub Ławreszuk"/><br /><sub><b>Jakub Ławreszuk</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=Jklawreszuk" title="Code">💻</a></td>
    </tr>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/Ethereal77"><img src="https://avatars.githubusercontent.com/u/8967302?v=4?s=100" width="100px;" alt="Mario Guerra"/><br /><sub><b>Mario Guerra</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=Ethereal77" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/tamamutu"><img src="https://avatars.githubusercontent.com/u/62577086?v=4?s=100" width="100px;" alt="tamamutu"/><br /><sub><b>tamamutu</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=tamamutu" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/IXLLEGACYIXL"><img src="https://avatars.githubusercontent.com/u/107197024?v=4?s=100" width="100px;" alt="IXLLEGACYIXL"/><br /><sub><b>IXLLEGACYIXL</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=IXLLEGACYIXL" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/arturoc"><img src="https://avatars.githubusercontent.com/u/48240?v=4?s=100" width="100px;" alt="arturo"/><br /><sub><b>arturo</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=arturoc" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/garychia"><img src="https://avatars.githubusercontent.com/u/88014292?v=4?s=100" width="100px;" alt="Chia-Hsiang Cheng"/><br /><sub><b>Chia-Hsiang Cheng</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=garychia" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://nicusorn5.github.io/"><img src="https://avatars.githubusercontent.com/u/20599225?v=4?s=100" width="100px;" alt="Nicolae Tugui"/><br /><sub><b>Nicolae Tugui</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=NicusorN5" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://mattiascibien.net"><img src="https://avatars.githubusercontent.com/u/1300681?v=4?s=100" width="100px;" alt="Mattias Cibien"/><br /><sub><b>Mattias Cibien</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=mattiascibien" title="Code">💻</a></td>
    </tr>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="http://cnonim.name"><img src="https://avatars.githubusercontent.com/u/523048?v=4?s=100" width="100px;" alt="Oleg Ageev"/><br /><sub><b>Oleg Ageev</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=cNoNim" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/SeleDreams"><img src="https://avatars.githubusercontent.com/u/16335601?v=4?s=100" width="100px;" alt="SeleDreams"/><br /><sub><b>SeleDreams</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=SeleDreams" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/acastrodev"><img src="https://avatars.githubusercontent.com/u/6575712?v=4?s=100" width="100px;" alt="Alexandre Castro"/><br /><sub><b>Alexandre Castro</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=acastrodev" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/SVNMLR"><img src="https://avatars.githubusercontent.com/u/44621949?v=4?s=100" width="100px;" alt="SVNMLR"/><br /><sub><b>SVNMLR</b></sub></a><br /><a href="#design-SVNMLR" title="Design">🎨</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://chroniclesofelyria.com"><img src="https://avatars.githubusercontent.com/u/17633767?v=4?s=100" width="100px;" alt="Jeromy Walsh"/><br /><sub><b>Jeromy Walsh</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=JeromyWalsh" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://pa.rh.am/"><img src="https://avatars.githubusercontent.com/u/7075456?v=4?s=100" width="100px;" alt="Parham Gholami"/><br /><sub><b>Parham Gholami</b></sub></a><br /><a href="#design-parhamgholami" title="Design">🎨</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/adrsch"><img src="https://avatars.githubusercontent.com/u/35346279?v=4?s=100" width="100px;" alt="adrsch"/><br /><sub><b>adrsch</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=adrsch" title="Code">💻</a></td>
    </tr>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/dotlogix"><img src="https://avatars.githubusercontent.com/u/16420200?v=4?s=100" width="100px;" alt="Alexander Schill"/><br /><sub><b>Alexander Schill</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=dotlogix" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/froce"><img src="https://avatars.githubusercontent.com/u/8515865?v=4?s=100" width="100px;" alt="froce"/><br /><sub><b>froce</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=froce" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://fydar.dev"><img src="https://avatars.githubusercontent.com/u/19309165?v=4?s=100" width="100px;" alt="Fydar"/><br /><sub><b>Fydar</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=fydar" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/MaximilianEmel"><img src="https://avatars.githubusercontent.com/u/19846453?v=4?s=100" width="100px;" alt="MaximilianEmel"/><br /><sub><b>MaximilianEmel</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=MaximilianEmel" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/Schossi"><img src="https://avatars.githubusercontent.com/u/8679168?v=4?s=100" width="100px;" alt="Schossi"/><br /><sub><b>Schossi</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=Schossi" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/ch3mbot"><img src="https://avatars.githubusercontent.com/u/110746303?v=4?s=100" width="100px;" alt="Dagan Hartmann"/><br /><sub><b>Dagan Hartmann</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=ch3mbot" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/Nicogo1705"><img src="https://avatars.githubusercontent.com/u/20603105?v=4?s=100" width="100px;" alt="nicogo.eth"/><br /><sub><b>nicogo.eth</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=Nicogo1705" title="Code">💻</a></td>
    </tr>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/ourabigdev"><img src="https://avatars.githubusercontent.com/u/147079928?v=4?s=100" width="100px;" alt="hatim ourahou"/><br /><sub><b>hatim ourahou</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=ourabigdev" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/kopffarben"><img src="https://avatars.githubusercontent.com/u/1833690?v=4?s=100" width="100px;" alt="kopffarben"/><br /><sub><b>kopffarben</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=kopffarben" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/Feralnex"><img src="https://avatars.githubusercontent.com/u/30673252?v=4?s=100" width="100px;" alt="Tomasz Czech"/><br /><sub><b>Tomasz Czech</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=Feralnex" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/TranquilAbyss"><img src="https://avatars.githubusercontent.com/u/2864849?v=4?s=100" width="100px;" alt="Tranquil Abyss"/><br /><sub><b>Tranquil Abyss</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=TranquilAbyss" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/levifmorais"><img src="https://avatars.githubusercontent.com/u/102878183?v=4?s=100" width="100px;" alt="Levi Ferreira"/><br /><sub><b>Levi Ferreira</b></sub></a><br /><a href="#design-levifmorais" title="Design">🎨</a></td>
      <td align="center" valign="top" width="14.28%"><a href="http://tylerkvochick.com"><img src="https://avatars.githubusercontent.com/u/12144028?v=4?s=100" width="100px;" alt="Tyler Kvochick"/><br /><sub><b>Tyler Kvochick</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=tymokvo" title="Documentation">📖</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/Arc-huangjingtong"><img src="https://avatars.githubusercontent.com/u/87562566?v=4?s=100" width="100px;" alt="Arc"/><br /><sub><b>Arc</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=Arc-huangjingtong" title="Code">💻</a></td>
    </tr>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/minktusk"><img src="https://avatars.githubusercontent.com/u/121324712?v=4?s=100" width="100px;" alt="minktusk"/><br /><sub><b>minktusk</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=minktusk" title="Code">💻</a> <a href="#content-minktusk" title="Content">🖋</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://www.timcassell.net/"><img src="https://avatars.githubusercontent.com/u/35501420?v=4?s=100" width="100px;" alt="Tim Cassell"/><br /><sub><b>Tim Cassell</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=timcassell" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="http://www.linkedin.com/in/dylan-loe"><img src="https://avatars.githubusercontent.com/u/18317814?v=4?s=100" width="100px;" alt="Dylan Loe"/><br /><sub><b>Dylan Loe</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=dloe" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/wrshield"><img src="https://avatars.githubusercontent.com/u/145876802?v=4?s=100" width="100px;" alt="Will S"/><br /><sub><b>Will S</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=wrshield" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/soorMSWE"><img src="https://avatars.githubusercontent.com/u/147351572?v=4?s=100" width="100px;" alt="Ryan Soo"/><br /><sub><b>Ryan Soo</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=soorMSWE" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/MechWarrior99"><img src="https://avatars.githubusercontent.com/u/8076495?v=4?s=100" width="100px;" alt="MechWarrior99"/><br /><sub><b>MechWarrior99</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=MechWarrior99" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/kristian15959"><img src="https://avatars.githubusercontent.com/u/8007327?v=4?s=100" width="100px;" alt="Proxid"/><br /><sub><b>Proxid</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=kristian15959" title="Code">💻</a></td>
    </tr>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="http://yerkoandrei.github.io"><img src="https://avatars.githubusercontent.com/u/19843418?v=4?s=100" width="100px;" alt="Yerko Andrei"/><br /><sub><b>Yerko Andrei</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=YerkoAndrei" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/ComputerSmoke"><img src="https://avatars.githubusercontent.com/u/22194459?v=4?s=100" width="100px;" alt="Addison Schmidt"/><br /><sub><b>Addison Schmidt</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=ComputerSmoke" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/timconner"><img src="https://avatars.githubusercontent.com/u/22841670?v=4?s=100" width="100px;" alt="Tim Conner"/><br /><sub><b>Tim Conner</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=timconner" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://www.caleblamcodes.dev/"><img src="https://avatars.githubusercontent.com/u/67606076?v=4?s=100" width="100px;" alt="Caleb L."/><br /><sub><b>Caleb L.</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=ClamEater14" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/net2cn"><img src="https://avatars.githubusercontent.com/u/6072596?v=4?s=100" width="100px;" alt="net2cn"/><br /><sub><b>net2cn</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=net2cn" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/laske185"><img src="https://avatars.githubusercontent.com/u/37439758?v=4?s=100" width="100px;" alt="Peter Laske"/><br /><sub><b>Peter Laske</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=laske185" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/MikhailArsentevTheSecond"><img src="https://avatars.githubusercontent.com/u/22962265?v=4?s=100" width="100px;" alt="Mikhail Arsentev"/><br /><sub><b>Mikhail Arsentev</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=MikhailArsentevTheSecond" title="Code">💻</a></td>
    </tr>
    <tr>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/hoelzl"><img src="https://avatars.githubusercontent.com/u/35998?v=4?s=100" width="100px;" alt="Matthias Hölzl"/><br /><sub><b>Matthias Hölzl</b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=hoelzl" title="Code">💻</a></td>
      <td align="center" valign="top" width="14.28%"><a href="https://github.com/kutal10"><img src="https://avatars.githubusercontent.com/u/36085864?v=4?s=100" width="100px;" alt="Rahul Pai "/><br /><sub><b>Rahul Pai </b></sub></a><br /><a href="https://github.com/stride3d/stride/commits?author=kutal10" title="Code">💻</a></td>
    </tr>
  </tbody>
</table>

<!-- markdownlint-restore -->
<!-- prettier-ignore-end -->

<!-- ALL-CONTRIBUTORS-LIST:END -->

This project follows the [all-contributors](https://github.com/all-contributors/all-contributors) specification. Contributions of any kind welcome!
