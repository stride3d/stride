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
        /// <summary>
        /// Borderless
        /// </summary>
        None = 0,
        
        /// <summary>
        /// Borders but not resizeable
        /// </summary>
        FixedSingle = 1,
        
        /// <summary>
        /// Borders and resizeable
        /// </summary>
        Sizable = 4,
    }
}
#endif
