// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Runtime.CompilerServices;

namespace Stride.Rendering
{
    /// <summary>
    /// Extensions for <see cref="RenderGroupMask"/>
    /// </summary>
    public static class RenderGroupMaskExtensions
    {
        /// <summary>
        /// Determines whether the group mask contains the specified group.
        /// </summary>
        /// <param name="mask">The mask.</param>
        /// <param name="group">The group.</param>
        /// <returns><c>true</c> if the group mask contains the specified group; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains(this RenderGroupMask mask, RenderGroup group)
        {
            return ((uint)mask & (1 << (int)group)) != 0;
        }
    }
}
