// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using System.Windows.Interop;
using Stride.Core.Presentation.Interop;

namespace Stride.Core.Assets.Editor.View.Behaviors
{
    internal class DragWindow : Window
    {
        public DragWindow()
        {
            AllowDrop = false;
            AllowsTransparency = true;
            Background = null;
            IsHitTestVisible = false;
            ShowActivated = false;
            ShowInTaskbar = false;
            SizeToContent = SizeToContent.WidthAndHeight;
            Style = null;
            Topmost = true;
            WindowStyle = WindowStyle.None;
            SourceInitialized += InitializeExtendedStyle;
        }

        private void InitializeExtendedStyle(object sender, EventArgs eventArgs)
        {
            var source = (HwndSource)PresentationSource.FromVisual(this);
            if (source == null)
                throw new InvalidOperationException("Unable to retrieve the HwndSource of this window");

            int extendedStyle = NativeHelper.GetWindowLong(source.Handle, NativeHelper.GWL_EXSTYLE);
            extendedStyle = (int)(extendedStyle | NativeHelper.WS_EX_TRANSPARENT | NativeHelper.WS_EX_LAYERED | NativeHelper.WS_EX_TOPMOST);
            NativeHelper.SetWindowLong(source.Handle, NativeHelper.GWL_EXSTYLE, extendedStyle);
        }
    }
}
