// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
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
        private readonly CaptureEdgeTracker<GamePadButton> buttonTracker;

        public abstract string Name { get; }
        public abstract Guid Id { get; }
        public abstract Guid ProductId { get; }
        /// <summary>
        /// The state of the gamepad as the game sees it: neutral while the gamepad is captured, and without buttons
        /// that were held when a capture ended until they are pressed again.
        /// </summary>
        public GamePadState State
        {
            get
            {
                if (CaptureState.IsMasked)
                    return default;

                var state = RawState;
                foreach (var button in buttonTracker.Suppressed)
                    state.Buttons &= ~button;
                return state;
            }
        }

        /// <summary>
        /// The unmasked state reported by the device.
        /// </summary>
        protected abstract GamePadState RawState { get; }
        public bool CanChangeIndex { get; protected set; } = true;
        public int Priority { get; set; }

        /// <inheritdoc/>
        public DeviceCaptureState CaptureState { get; } = new DeviceCaptureState();

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

        public Core.Collections.IReadOnlySet<GamePadButton> PressedButtons { get; }
        public Core.Collections.IReadOnlySet<GamePadButton> ReleasedButtons { get; }
        public Core.Collections.IReadOnlySet<GamePadButton> DownButtons { get; }

        public abstract IInputSource Source { get; }

        public event EventHandler<GamePadIndexChangedEventArgs> IndexChanged;

        public abstract void Update(List<InputEvent> inputEvents);
        public abstract void SetVibration(float smallLeft, float smallRight, float largeLeft, float largeRight);

        protected GamePadDeviceBase()
        {
            buttonTracker = new CaptureEdgeTracker<GamePadButton>(CaptureState, new ReadOnlySet<GamePadButton>(downButtons), new ReadOnlySet<GamePadButton>(pressedButtons), new ReadOnlySet<GamePadButton>(releasedButtons));
            CaptureState.MaskChanged += buttonTracker.OnMaskChanged;
            CaptureState.DeviceUpdated += buttonTracker.AfterDeviceUpdate;
            CaptureState.DeviceMasksOwnState = true;

            PressedButtons = buttonTracker.Pressed;
            ReleasedButtons = buttonTracker.Released;
            DownButtons = buttonTracker.Down;
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
