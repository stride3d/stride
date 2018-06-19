// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if XENKO_PLATFORM_WINDOWS_DESKTOP && (XENKO_UI_WINFORMS || XENKO_UI_WPF)
using SharpDX.DirectInput;

namespace Xenko.Input
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