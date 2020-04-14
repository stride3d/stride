// Copyright (c) Xenko contributors (https://xenko.com)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xenko.Core.Mathematics;

namespace Xenko.Input
{
    /// <summary>
    /// Simulation of PointerEvents
    /// </summary>
    public class PointerSimulated : PointerDeviceBase
    {
        public PointerSimulated(InputSourceSimulated source)
        {
            Priority = -1000;
            SetSurfaceSize(Vector2.One);
            Source = source;
        }

        public override string Name => "Simulated Pointer";

        public override Guid Id => new Guid("8D527970-EB53-4392-AFBB-CB08CFF95143");

        public override IInputSource Source { get; }

        public new PointerDeviceState PointerState => base.PointerState;

        public override void Update(List<InputEvent> inputEvents)
        {
            base.Update(inputEvents);
        }

        public void SimulatePointer(PointerEventType pointerEventType, Vector2 position, int id = 0)
        {
            PointerState.PointerInputEvents.Add(new PointerDeviceState.InputEvent { Id = id, Position = position, Type = pointerEventType });
        }

        //shortcuts for convenience
        public void MovePointer(Vector2 position, int id = 0)
        {
            SimulatePointer(PointerEventType.Moved, position, id);
        }

        public void PressPointer(Vector2 position, int id = 0)
        {
            SimulatePointer(PointerEventType.Pressed, position, id);
        }

        public void ReleasePointer(Vector2 position, int id = 0)
        {
            SimulatePointer(PointerEventType.Released, position, id);
        }

        public void CancelPointer(Vector2 position, int id = 0)
        {
            SimulatePointer(PointerEventType.Canceled, position, id);
        }
    }
}
