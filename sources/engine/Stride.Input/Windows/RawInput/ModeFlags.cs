// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Input.RawInput
{
    [Flags]
    enum ModeFlags
    {
        RIDEV_REMOVE        = 0x00000001,
        RIDEV_EXCLUDE       = 0x00000010,
        RIDEV_PAGEONLY      = 0x00000020,
        RIDEV_NOLEGACY      = 0x00000030,
        RIDEV_INPUTSINK     = 0x00000100,
        RIDEV_CAPTUREMOUSE  = 0x00000200,
        RIDEV_NOHOTKEYS     = 0x00000200,
        RIDEV_APPKEYS       = 0x00000400,
        RIDEV_EXINPUTSINK   = 0x00001000,
        RIDEV_DEVNOTIFY     = 0x00002000,
    }
}
