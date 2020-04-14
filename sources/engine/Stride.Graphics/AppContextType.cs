// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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

namespace Xenko.Games
{
    /// <summary>
    /// Type of a <see cref="GameContext"/>.
    /// </summary>
    public enum AppContextType
    {
        /// <summary>
        /// Game running on desktop in a form or <see cref="System.Windows.Forms.Control"/>.
        /// </summary>
        Desktop,

        /// <summary>
        /// Game running on desktop in a SDL window.
        /// </summary>
        DesktopSDL,

        /// <summary>
        /// Game running on desktop in a WPF window through a D3DImage.
        /// </summary>
        DesktopWpf,

        /// <summary>
        /// Game running on desktop in an OpenTK form.
        /// </summary>
        DesktopOpenTK,

        /// <summary>
        /// Game running on Android in an AndroidXenkoGameView.
        /// </summary>
        Android,

        /// <summary>
        /// Game running on UWP in a Xaml SwapChainPanel.
        /// </summary>
        UWPXaml,

        /// <summary>
        /// Game running on UWP in a CoreWindow.
        /// </summary>
        UWPCoreWindow,

#pragma warning disable SA1300 // Element must begin with upper-case letter
        /// <summary>
        /// Game running on iOS in a iPhoneOSGameView.
        /// </summary>
        iOS,
#pragma warning restore SA1300 // Element must begin with upper-case letter
    }
}
