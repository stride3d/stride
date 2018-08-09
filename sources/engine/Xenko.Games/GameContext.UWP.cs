// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
//
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
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
#if XENKO_PLATFORM_UWP
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace Xenko.Games
{
    public class GameContextUWPXaml : GameContextWindows<SwapChainPanel>
    {
        // Used internally by systems such as UI to capture input in a TextBox
        internal TextBox EditTextBox = new TextBox { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(10, 0, 10, 0)};

        /// <inheritDoc/>
        public GameContextUWPXaml(SwapChainPanel control, int requestedWidth = 0, int requestedHeight = 0)
            : base (control ?? new SwapChainPanel(), requestedWidth, requestedHeight)
        {
            ContextType = AppContextType.UWPXaml;
        }
    }

    public class GameContextUWPCoreWindow : GameContextWindows<CoreWindow>
    {
        /// <inheritDoc/>
        public GameContextUWPCoreWindow(CoreWindow control, int requestedWidth = 0, int requestedHeight = 0, bool isWindowsMixedReality = false)
            : base(control ?? CoreWindow.GetForCurrentThread(), requestedWidth, requestedHeight)
        {
            ContextType = AppContextType.UWPCoreWindow;
            IsWindowsMixedReality = isWindowsMixedReality;
        }

        internal bool IsWindowsMixedReality { get; }
    }
}
#endif
