// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Collections;

namespace Stride.Input
{
    /// <summary>
    /// Base class for gamepads, contains common functionality for gamepad devices
    /// </summary>
    public abstract class GameControllerDeviceBase : IGameControllerDevice
    {
        private readonly HashSet<int> pressedButtons = new HashSet<int>();
        private readonly HashSet<int> releasedButtons = new HashSet<int>();
        private readonly HashSet<int> downButtons = new HashSet<int>();

        private readonly List<InputEvent> events = new List<InputEvent>();

        protected bool[] ButtonStates;
        protected float[] AxisStates;
        protected Direction[] DirectionStates;

        protected GameControllerDeviceBase()
        {
            PressedButtons = new ReadOnlySet<int>(pressedButtons);
            ReleasedButtons = new ReadOnlySet<int>(releasedButtons);
            DownButtons = new ReadOnlySet<int>(downButtons);
        }

        public abstract string Name { get; }
        public abstract Guid Id { get; }
        public virtual Guid ProductId => Id;

        public int Priority { get; set; }

        public abstract IInputSource Source { get; }
        public abstract IReadOnlyList<GameControllerButtonInfo> ButtonInfos { get; }
        public abstract IReadOnlyList<GameControllerAxisInfo> AxisInfos { get; }
        public abstract IReadOnlyList<GameControllerDirectionInfo> DirectionInfos { get; }

        public IReadOnlySet<int> PressedButtons { get; }
        public IReadOnlySet<int> ReleasedButtons { get; }
        public IReadOnlySet<int> DownButtons { get; }

        /// <summary>
        /// Creates the correct amount of states based on the amount of object infos that are set
        /// </summary>
        protected void InitializeButtonStates()
        {
            ButtonStates = new bool[ButtonInfos.Count];
            AxisStates = new float[AxisInfos.Count];
            DirectionStates = new Direction[DirectionInfos.Count];
        }
        
        public virtual float GetAxis(int index)
        {
            if (index < 0 || index > AxisStates.Length)
                return 0.0f;

            return AxisStates[index];
        }
        
        public virtual Direction GetDirection(int index)
        {
            return DirectionStates[index];
        }

        /// <summary>
        /// Raise gamepad events collected by Handle... functions
        /// </summary>
        public virtual void Update(List<InputEvent> inputEvents)
        {
            pressedButtons.Clear();
            releasedButtons.Clear();

            // Collect events from queue
            foreach (var evt in events)
            {
                inputEvents.Add(evt);

                var buttonEvent = evt as GameControllerButtonEvent;
                if (buttonEvent != null)
                {
                    if (buttonEvent.IsDown)
                    {
                        pressedButtons.Add(buttonEvent.Index);
                        downButtons.Add(buttonEvent.Index);
                    }
                    else
                    {
                        releasedButtons.Add(buttonEvent.Index);
                        downButtons.Remove(buttonEvent.Index);
                    }
                }
            }
            events.Clear();
        }

        protected void HandleButton(int index, bool state)
        {
            if (index < 0 || index > ButtonStates.Length)
                throw new IndexOutOfRangeException();

            if (ButtonStates[index] != state)
            {
                ButtonStates[index] = state;
                var buttonEvent = InputEventPool<GameControllerButtonEvent>.GetOrCreate(this);
                buttonEvent.IsDown = state;
                buttonEvent.Index = index;
                events.Add(buttonEvent);
            }
        }

        protected void HandleAxis(int index, float state)
        {
            if (index < 0 || index > AxisStates.Length)
                throw new IndexOutOfRangeException();

            if (AxisStates[index] != state)
            {
                AxisStates[index] = state;
                var axisEvent = InputEventPool<GameControllerAxisEvent>.GetOrCreate(this);
                axisEvent.Value = state;
                axisEvent.Index = index;
                events.Add(axisEvent);
            }
        }

        protected void HandleDirection(int index, Direction state)
        {
            if (index < 0 || index > DirectionStates.Length)
                throw new IndexOutOfRangeException();

            if (DirectionStates[index] != state)
            {
                DirectionStates[index] = state;
                var directionEvent = InputEventPool<GameControllerDirectionEvent>.GetOrCreate(this);
                directionEvent.Index = index;
                directionEvent.Direction = state;
                events.Add(directionEvent);
            }
        }
    }
}