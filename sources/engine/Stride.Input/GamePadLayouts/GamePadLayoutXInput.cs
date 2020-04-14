// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Input
{
    /// <summary>
    /// Layout for XInput devices so that they can be used by SDL or other systems that do not have the XInput API but do support joysticks in some other way
    /// </summary>
    public class GamePadLayoutXInput : GamePadLayout
    {
        private static readonly Guid commonProductId;

        static GamePadLayoutXInput()
        {
            byte[] pidBytes = new byte[16];
            pidBytes[0] = (byte)'x';
            pidBytes[1] = (byte)'i';
            pidBytes[2] = (byte)'n';
            pidBytes[3] = (byte)'p';
            pidBytes[4] = (byte)'u';
            pidBytes[5] = (byte)'t';
            commonProductId = new Guid(pidBytes);
        }

        public GamePadLayoutXInput()
        {
            AddButtonToButton(7, GamePadButton.Start);
            AddButtonToButton(6, GamePadButton.Back);
            AddButtonToButton(8, GamePadButton.LeftThumb);
            AddButtonToButton(9, GamePadButton.RightThumb);
            AddButtonToButton(4, GamePadButton.LeftShoulder);
            AddButtonToButton(5, GamePadButton.RightShoulder);
            AddButtonToButton(0, GamePadButton.A);
            AddButtonToButton(1, GamePadButton.B);
            AddButtonToButton(2, GamePadButton.X);
            AddButtonToButton(3, GamePadButton.Y);
            AddAxisToAxis(0, GamePadAxis.LeftThumbX);
            AddAxisToAxis(1, GamePadAxis.LeftThumbY, true);
            AddAxisToAxis(3, GamePadAxis.RightThumbX);
            AddAxisToAxis(4, GamePadAxis.RightThumbY, true);
            AddAxisToAxis(2, GamePadAxis.LeftTrigger, remap: true);
            AddAxisToAxis(5, GamePadAxis.RightTrigger, remap: true);
        }

        public override bool MatchDevice(IInputSource source, IGameControllerDevice device)
        {
            return CompareProductId(device.ProductId, commonProductId, 5);
        }
    }
}