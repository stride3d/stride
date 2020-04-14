// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Collections;

namespace Stride.Input
{
    /// <summary>
    /// Base class for keyboard devices
    /// </summary>
    public abstract class KeyboardDeviceBase : IKeyboardDevice
    {
        private readonly HashSet<Keys> pressedKeys = new HashSet<Keys>();
        private readonly HashSet<Keys> releasedKeys = new HashSet<Keys>();
        private readonly HashSet<Keys> downKeys = new HashSet<Keys>();

        protected readonly List<KeyEvent> Events = new List<KeyEvent>();

        public readonly Dictionary<Keys, int> KeyRepeats = new Dictionary<Keys, int>();

        protected KeyboardDeviceBase()
        {
            PressedKeys = new ReadOnlySet<Keys>(pressedKeys);
            ReleasedKeys = new ReadOnlySet<Keys>(releasedKeys);
            DownKeys = new ReadOnlySet<Keys>(downKeys);
        }

        public IReadOnlySet<Keys> PressedKeys { get; }
        public IReadOnlySet<Keys> ReleasedKeys { get; }
        public IReadOnlySet<Keys> DownKeys { get; }

        public abstract string Name { get; }

        public abstract Guid Id { get; }

        public int Priority { get; set; }

        public abstract IInputSource Source { get; }

        public virtual void Update(List<InputEvent> inputEvents)
        {
            pressedKeys.Clear();
            releasedKeys.Clear();
            
            // Fire events
            foreach (var keyEvent in Events)
            {
                inputEvents.Add(keyEvent);
                
                if (keyEvent != null)
                {
                    if (keyEvent.IsDown)
                    {
                        pressedKeys.Add(keyEvent.Key);
                    }
                    else
                    {
                        releasedKeys.Add(keyEvent.Key);
                    }
                }
            }
            Events.Clear();
        }
        
        public void HandleKeyDown(Keys key)
        {
            // Increment repeat count on subsequent down events
            int repeatCount;
            if (KeyRepeats.TryGetValue(key, out repeatCount))
            {
                KeyRepeats[key] = ++repeatCount;
            }
            else
            {
                KeyRepeats.Add(key, repeatCount);
                downKeys.Add(key);
            }

            var keyEvent = InputEventPool<KeyEvent>.GetOrCreate(this);
            keyEvent.IsDown = true;
            keyEvent.Key = key;
            keyEvent.RepeatCount = repeatCount;
            Events.Add(keyEvent);
        }

        public void HandleKeyUp(Keys key)
        {
            // Prevent duplicate up events
            if (!KeyRepeats.ContainsKey(key))
                return;

            KeyRepeats.Remove(key);
            downKeys.Remove(key);
            var keyEvent = InputEventPool<KeyEvent>.GetOrCreate(this);
            keyEvent.IsDown = false;
            keyEvent.Key = key;
            keyEvent.RepeatCount = 0;
            Events.Add(keyEvent);
        }
    }
}