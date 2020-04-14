// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_UI_SDL
namespace Stride.Graphics.SDL
{
    /// <summary>
    /// Set of border style to mimic the Windows forms one. We actually only show <see cref="FixedSingle"/> and <see cref="Sizable"/> as the
    /// other values don't make sense in a purely SDL context.
    /// </summary>
    public enum FormBorderStyle
    {
        None = 0,
        FixedSingle = 1,
        Sizable = 4,
    }
}
#endif
