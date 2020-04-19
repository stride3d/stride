// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_WINDOWS_DESKTOP && (STRIDE_UI_WINFORMS || STRIDE_UI_WPF)
using SharpDX.DirectInput;

namespace Stride.Input
{
    /// <summary>
    /// Provides easy operations on <see cref="DeviceObjectTypeFlags"/>
    /// </summary>
    internal static class DeviceObjectIdExtensions
    {
        public static bool HasFlags(this DeviceObjectId objectId, DeviceObjectTypeFlags flags)
        {
            return ((int)objectId.Flags & (int)flags) == (int)flags;
        }

        public static bool HasAnyFlag(this DeviceObjectId objectId, DeviceObjectTypeFlags flags)
        {
            return ((int)objectId.Flags & (int)flags) != 0;
        }
    }
}
#endif