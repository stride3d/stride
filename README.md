![Xenko](https://xenko.com/images/external/xenko-logo-side.png)
=======

欢迎来到Xenko源代码存储库!

Xenko是用于逼真的渲染和VR的开源C#游戏引擎。
该引擎是高度模块化的，旨在为游戏制造商提供更大的开发灵活性。
Xenko带有一个编辑器，可让您以直观的方式创建和管理游戏或应用程序的内容。

![Xenko编辑器](https://xenko.com/images/external/script-editor.png)

要了解有关Xenko的更多信息，请访问[xenko.com](https://xenko.com/)。

## 执照

除非另有说明(例如，对于从其他项目复制的某些文件)，[enko](LICENSE.md)涵盖Xenko。

您可以在[此处](THIRD％20PARTY.md)找到第三方项目的列表。

投稿人需要签署以下[投稿许可协议](docs/ContributorLicenseAgreement.md)。

## 文档

查找有关Xenko的说明和信息：
* [Xenko手册](https://doc.xenko.com/latest/manual/index.html)
* [API参考](https://doc.xenko.com/latest/api/index.html)
* [发行说明](https://doc.xenko.com/latest/ReleaseNotes/index.html)

## 社区

寻求帮助或报告问题：
* 与社区聊天[![在https://gitter.im/xenko3d/xenko中加入聊天](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/xenko3d/xenko?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)
* [在我们的论坛上讨论主题](http://forums.xenko.com/)
* [报告引擎问题](https://github.com/xenko3d/xenko/issues)
* [捐赠以支持该项目](https://www.patreon.com/xenko)

## 从源头建造

### 先决条件

1. [Git](https://git-scm.com/downloads)(包括LFS的最新版本，或单独安装[Git LFS](https://git-lfs.github.com/))。
2. [Visual Studio 2017](https://www.visualstudio.com/downloads/)，具有以下工作负载：
  * `.NET桌面开发`
    * 如果您的操作系统是`Windows 10`，请在`.NET桌面开发`的可选组件中添加`.NET Framework 4.6.2 SDK`。
    * 如果您的操作系统是`Windows 7`: [.NET 4.6.2 开发工具](https://www.microsoft.com/net/download/thank-you/net462-developer-pack))
  * `使用C ++进行桌面开发`
  * `.NET Core跨平台开发`
  * 可选(针对UWP)：`通用Windows平台开发`
  * 可选(针对iOS/Android)：使用 `Mobile development with .NET` 和 `Android NDK R13B+` 单独组件
3. [FBX SDK 2019.0 VS2015](https://www.autodesk.com/developer-network/platform-technologies/fbx-sdk-2019-0)

### 建立Xenko

1. 克隆Xenko：`git clone https：// github.com/xenko3d/xenko.git`
2. 将`<XenkoDir>`环境变量设置为指向您的`<XenkoDir>`
3. 使用Visual Studio 2017打开`<XenkoDir>\build\Xenko.sln`并进行构建。
4. 打开`<XenkoDir>\samples\XenkoSamples.sln`，然后播放示例。
5. (可选)打开并构建`Xenko.Android.sln`，`Xenko.iOS.sln`等。

### 贡献准则

请查看我们的[贡献准则](docs/CONTRIBUTING.md)。

### 构建状态
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
