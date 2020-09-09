// Copyright (c) Stride contributors (https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Input.RawInput
{
    internal enum UsageId: ushort
    {
        HID_USAGE_GENERIC_POINTER = 0x01,
        HID_USAGE_GENERIC_MOUSE = 0x02,
        HID_USAGE_GENERIC_JOYSTICK = 0x04,
        HID_USAGE_GENERIC_GAMEPAD = 0x05,
        HID_USAGE_GENERIC_KEYBOARD = 0x06,
        HID_USAGE_GENERIC_KEYPAD = 0x07,
        HID_USAGE_GENERIC_MULTI_AXIS_CONTROLLER = 0x08,
    }
}
