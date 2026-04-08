// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2012 SharpDX - Alexandre Mutel
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

namespace Stride.Games;

/// <summary>
///   Defines the type of a <c>GameContext</c>, which usually determines the platform, or
///   a combination of platform and UI/presentation framework.
/// </summary>
public enum AppContextType
{
    /// <summary>
    ///   The <c>Game</c> runs on desktop in a Windows Forms' <c>Form</c> or <c>Control</c>.
    /// </summary>
    Desktop,

    /// <summary>
    ///   The <c>Game</c> runs on desktop in a SDL window.
    /// </summary>
    DesktopSDL,

    /// <summary>
    ///   The <c>Game</c> runs on desktop in a WPF window through a <c>D3DImage</c>.
    /// </summary>
    DesktopWpf,

    /// <summary>
    ///   The <c>Game</c> runs on an Android device in an <c>AndroidStrideGameView</c>.
    /// </summary>
    Android,

    /// <summary>
    ///   The <c>Game</c> runs on UWP (<em>Universal Windows Platform</em>) in a <em>Xaml</em>
    ///   <c>SwapChainPanel</c>.
    /// </summary>
    UWPXaml,

    /// <summary>
    ///   The <c>Game</c> runs on UWP (<em>Universal Windows Platform</em>) in a <c>CoreWindow</c>.
    /// </summary>
    UWPCoreWindow,

#pragma warning disable SA1300 // Element must begin with upper-case letter
    /// <summary>
    ///   The <c>Game</c> runs on an iOS device in an <c>iPhoneOSGameView</c>.
    /// </summary>
    iOS
#pragma warning restore SA1300 // Element must begin with upper-case letter
}
