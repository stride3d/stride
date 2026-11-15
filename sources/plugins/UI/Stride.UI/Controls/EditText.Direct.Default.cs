// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

// Stub impls (no native widget, in-engine rendering) — same scope as EditText.Direct.cs.
#if STRIDE_PLATFORM_DESKTOP || STRIDE_PLATFORM_IOS

namespace Stride.UI.Controls
{
    public partial class EditText
    {
        private static void InitializeStaticImpl()
        {
        }

        private void InitializeImpl()
        {
        }

        private int GetLineCountImpl()
        {
            if (Font == null)
                return 1;

            return text.Split('\n').Length;
        }

        private void OnMaxLinesChangedImpl()
        {
        }

        private void OnMinLinesChangedImpl()
        {
        }

        private void UpdateTextToEditImpl()
        {
        }

        private void UpdateInputTypeImpl()
        {
        }

        private void UpdateSelectionFromEditImpl()
        {
        }

        private void UpdateSelectionToEditImpl()
        {
        }

        private void OnTouchUpImpl(TouchEventArgs args)
        {
        }
    }
}

#endif
