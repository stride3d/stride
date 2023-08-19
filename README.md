![Stride](sources/data/images/Logo/stride-logo-readme.png)

[![Join the chat at https://discord.gg/f6aerfE](https://img.shields.io/discord/500285081265635328.svg?style=flat&logo=discord&label=discord)](https://discord.gg/f6aerfE)
<!-- ALL-CONTRIBUTORS-BADGE:START - Do not remove or modify this section -->
[![All Contributors](https://img.shields.io/badge/all_contributors-39-orange.svg?style=flat-square)](#contributors-)
<!-- ALL-CONTRIBUTORS-BADGE:END -->
[![Financial sponsors](https://img.shields.io/opencollective/all/stride3d)](https://opencollective.com/stride3d)


Welcome to the Stride source code repository!

Stride is an open-source C# game engine for realistic rendering and VR. 
The engine is highly modular and aims at giving game makers more flexibility in their development.
Stride comes with an editor that allows you to create and manage the content of your games or applications visually and intuitively.

![Stride Editor](https://stride3d.net/images/external/script-editor.png)

To learn more about Stride, visit [stride3d.net](https://stride3d.net/).

## License and governance
### .NET Foundation
This project is supported by the [.NET Foundation](https://dotnetfoundation.org).

### License
Stride is covered by the [MIT License](LICENSE.md) unless stated otherwise (i.e. for some files that are copied from other projects).
You can find the list of third party projects [here](THIRD%20PARTY.md).
Contributors need to sign the following [Contribution License Agreement](docs/ContributorLicenseAgreement.md).

### Code of conduct
Stride being a [.NET Foundation](https://www.dotnetfoundation.org/) project, it has adopted the code of conduct defined by the Contributor Covenant to clarify expected behavior in our community.
For more information see the [.NET Foundation Code of Conduct](https://dotnetfoundation.org/code-of-conduct). 

### Earn money by contributing
If you are a developer with solid experience in C#, rendering techniques, or game development, we want to hire you! We have allocated funds from supporters on OpenCollective and can pay for work on certain projects. [More info about this here](https://github.com/stride3d/stride/wiki/Bounty).

## Documentation

Find explanations and information about Stride:
* [Stride Manual](https://doc.stride3d.net/latest/manual/index.html)
* [Tutorials](https://doc.stride3d.net/latest/en/tutorials/)
* [API Reference](https://doc.stride3d.net/latest/api/index.html)
* [Release Notes](https://doc.stride3d.net/latest/ReleaseNotes/index.html)

## Community

Ask for help or report issues:
* [Chat with the community on Discord](https://discord.gg/f6aerfE)
* [Discuss topics on Github discussions](https://github.com/stride3d/stride/discussions)
* [Report engine issues](https://github.com/stride3d/stride/issues)
* [Donate to support the project](https://opencollective.com/stride3d/) [![Open collective backers](https://img.shields.io/opencollective/all/stride3d)](https://opencollective.com/stride3d)
* [List of Projects made by users](https://github.com/stride3d/stride/wiki/Community-Projects)
* [Localization](docs/localization.md)

## Building from source

### Prerequisites

1. **Latest** [Git](https://git-scm.com/downloads) **with Large File Support** selected in the setup on the components dialog.
2. [Visual Studio 2022](https://www.visualstudio.com/downloads/) with the following workloads:
  * `.NET desktop development` with `.NET Framework 4.7.2 targeting pack`
  * `Desktop development with C++` with
    * `Windows 10 SDK (10.0.18362.0)` (it's currently enabled by default but it might change)
    * `MSVC v143 - VS2022 C++ x64/x86 build tools (v14.30)` or later version (should be enabled by default)
    * `C++/CLI support for v143 build tools (v14.30)` or later version **(not enabled by default)**
  * Optional (to target UWP): `Universal Windows Platform development` with
    * `Windows 10 SDK (10.0.18362.0)` or later version
    * `MSVC v143 - VS2022 C++ ARM build tools (v14.30)` or later version **(not enabled by default)**
  * Optional (to target iOS/Android): `Mobile development with .NET` and `Android SDK setup (API level 27)` individual component, then in Visual Studio go to `Tools > Android > Android SDK Manager` and install `NDK` (version 19+) from `Tools` tab.
3. **[FBX SDK 2019.0 VS2015](https://www.autodesk.com/developer-network/platform-technologies/fbx-sdk-2019-0)**

### Build Stride

1. Open a command prompt, point it to a directory and clone Stride to it: `git lfs clone https://github.com/stride3d/stride.git`
   - Note that when you use GitHub -> Code -> Download ZIP, this doesn't support Large File Support ```lfs```, make sure you use the command above or that your git client does it for you
2. Open `<StrideDir>\build\Stride.sln` with Visual Studio 2022 and build `Stride.GameStudio` in the 60-Editor solution folder (it should be the default startup project) or run it from VS's toolbar.
   - Optionally, open and build `Stride.Android.sln`, `Stride.iOS.sln`, etc.

#### Build Stride without Visual Studio

1. Install [Visual Studio Build Tools](https://aka.ms/vs/17/release/vs_BuildTools.exe) with the same prerequisites listed above
2. Add MSBuild's directory to your system's *PATH* (ex: `C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin`)
3. Open a command prompt, point it to a directory and clone Stride to it: `git clone https://github.com/stride3d/stride.git`
4. Navigate to `/Build` with the command prompt, input `msbuild /t:Restore Stride.sln` then `compile.bat`

If building failed:
* If you skipped one of the `Prerequisites` thinking that you already have the latest version, update to the latest anyway just to be sure.
* Visual Studio might have issues properly building if an anterior version is present alongside 2022. If you want to keep those version make sure that they are up to date and that you are building Stride through VS 2022.
* Your system's *PATH* should not contain older versions of MSBuild (ex: `...\Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin` should be removed)
* Some changes might require a system reboot, try that if you haven't yet.
* Make sure that Git, Git LFS and Visual Studio can access the internet.
* Close VS, clear the nuget cache (in your cmd `dotnet nuget locals all --clear`), delete the hidden `.vs` folder inside `\build` and the files inside `bin\packages`, kill any msbuild and other vs processes, build the whole solution then build and run GameStudio.

Do note that test solutions might fail but it should not prevent you from building `Stride.GameStudio`.

### Contribution Guidelines

Please check our [Contributing Guidelines](docs/CONTRIBUTING.md).

### Build Status

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

## Contributors ✨

Thanks goes to these wonderful people ([emoji key](https://allcontributors.org/docs/en/emoji-key)):

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
    </tr>
  </tbody>
</table>

<!-- markdownlint-restore -->
<!-- prettier-ignore-end -->

<!-- ALL-CONTRIBUTORS-LIST:END -->

This project follows the [all-contributors](https://github.com/all-contributors/all-contributors) specification. Contributions of any kind welcome!
