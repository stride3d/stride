// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if (STRIDE_UI_WINFORMS || STRIDE_UI_WPF)
using System.Runtime.InteropServices;

namespace Stride.Input.RawInput
{
    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect
    {
        public int left;
        public int top;
        public int right;
        public int bottom;

        public Rect(int left, int top, int right, int bottom)
        {
            this.left = left;
            this.top = top;
            this.right = right;
            this.bottom = bottom;
        }
    }
}
#endif
