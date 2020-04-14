// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_WINDOWS_DESKTOP && (STRIDE_UI_WINFORMS || STRIDE_UI_WPF)
using System;

namespace Stride.Input
{
    /// <summary>
    /// A known gamepad that uses DirectInput as a driver
    /// </summary>
    internal class GamePadDirectInput : GamePadFromLayout, IGamePadDevice, IDisposable
    {
        private GameControllerDirectInput controller;

        public GamePadDirectInput(InputSourceWindowsDirectInput source, InputManager inputManager, GameControllerDirectInput controller, GamePadLayout layout)
            : base(inputManager, controller, layout)
        {
            this.controller = controller;
            Source = source;
            Name = controller.Name;
            Id = controller.Id;
            ProductId = controller.ProductId;
        }

        public void Dispose()
        {
            controller.Dispose();
        }

        public new int Index
        {
            get { return base.Index; }
            set { SetIndexInternal(value, false); }
        }

        public override string Name { get; }

        public override Guid Id { get; }

        public override Guid ProductId { get; }

        public override IInputSource Source { get; }

        public override void SetVibration(float smallLeft, float smallRight, float largeLeft, float largeRight)
        {
            // No vibration support in directinput gamepads
        }
    }
}

#endif