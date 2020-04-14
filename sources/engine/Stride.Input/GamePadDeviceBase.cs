// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Collections;

namespace Stride.Input
{
    public abstract class GamePadDeviceBase : IGamePadDevice
    {
        private readonly HashSet<GamePadButton> releasedButtons = new HashSet<GamePadButton>();
        private readonly HashSet<GamePadButton> pressedButtons = new HashSet<GamePadButton>();
        private readonly HashSet<GamePadButton> downButtons = new HashSet<GamePadButton>();
        private int index;

        public abstract string Name { get; }
        public abstract Guid Id { get; }
        public abstract Guid ProductId { get; }
        public abstract GamePadState State { get; }
        public bool CanChangeIndex { get; protected set; } = true;
        public int Priority { get; set; }

        public int Index
        {
            get { return index; }
            set
            {
                if (!CanChangeIndex)
                    throw new InvalidOperationException("This GamePad's index can not be changed");
                SetIndexInternal(value, false);
            }
        }

        public IReadOnlySet<GamePadButton> PressedButtons { get; }
        public IReadOnlySet<GamePadButton> ReleasedButtons { get; }
        public IReadOnlySet<GamePadButton> DownButtons { get; }

        public abstract IInputSource Source { get; }

        public event EventHandler<GamePadIndexChangedEventArgs> IndexChanged;

        public abstract void Update(List<InputEvent> inputEvents);
        public abstract void SetVibration(float smallLeft, float smallRight, float largeLeft, float largeRight);

        protected GamePadDeviceBase()
        {
            PressedButtons = new ReadOnlySet<GamePadButton>(pressedButtons);
            ReleasedButtons = new ReadOnlySet<GamePadButton>(releasedButtons);
            DownButtons = new ReadOnlySet<GamePadButton>(downButtons);
        }

        protected void SetIndexInternal(int newIndex, bool isDeviceSideChange = true)
        {
            if (this.index != newIndex)
            {
                this.index = newIndex;
                IndexChanged?.Invoke(this, new GamePadIndexChangedEventArgs() { Index = newIndex, IsDeviceSideChange = isDeviceSideChange });
            }
        }
        
        /// <summary>
        /// Clears previous Pressed/Released states
        /// </summary>
        protected void ClearButtonStates()
        {
            pressedButtons.Clear();
            releasedButtons.Clear();
        }

        /// <summary>
        /// Updates Pressed/Released/Down collections
        /// </summary>
        protected void UpdateButtonState(GamePadButtonEvent evt)
        {
            if (evt.IsDown && !downButtons.Contains(evt.Button))
            {
                pressedButtons.Add(evt.Button);
                downButtons.Add(evt.Button);
            }
            else if (!evt.IsDown && downButtons.Contains(evt.Button))
            {
                releasedButtons.Add(evt.Button);
                downButtons.Remove(evt.Button);
            }
        }
    }
}
