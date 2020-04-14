// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_WINDOWS_DESKTOP && (STRIDE_UI_WINFORMS || STRIDE_UI_WPF)
using SharpDX.DirectInput;

namespace Stride.Input
{
    internal class DirectInputState : IDeviceState<RawJoystickState, JoystickUpdate>
    {
        public bool[] Buttons = new bool[128];
        public float[] Axes = new float[8];
        public int[] PovControllers = new int[4];

        public unsafe void MarshalFrom(ref RawJoystickState value)
        {
            fixed (int* axesPtr = &value.X)
            {
                for (int i = 0; i < 8; i++)
                    Axes[i] = axesPtr[i] / 65535.0f;
            }
            fixed (byte* buttonsPtr = value.Buttons)
            {
                for (int i = 0; i < Buttons.Length; i++)
                    Buttons[i] = buttonsPtr[i] != 0;
            }
            fixed (int* povPtr = value.PointOfViewControllers)
            {
                for (int i = 0; i < PovControllers.Length; i++)
                    PovControllers[i] = povPtr[i];
            }
        }

        public void Update(JoystickUpdate update)
        {
        }
    }
}
#endif
