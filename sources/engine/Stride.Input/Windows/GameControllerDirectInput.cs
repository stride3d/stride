// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_WINDOWS_DESKTOP && (STRIDE_UI_WINFORMS || STRIDE_UI_WPF)
using System;
using System.Collections.Generic;
using SharpDX;
using SharpDX.DirectInput;

namespace Stride.Input
{
    internal class GameControllerDirectInput : GameControllerDeviceBase, IDisposable
    {
        private static readonly Dictionary<Guid, int> GuidToAxisOffsets = new Dictionary<Guid, int>
        {
            [ObjectGuid.XAxis] = 0,
            [ObjectGuid.YAxis] = 1,
            [ObjectGuid.ZAxis] = 2,
            [ObjectGuid.RxAxis] = 3,
            [ObjectGuid.RyAxis] = 4,
            [ObjectGuid.RzAxis] = 5,
            [ObjectGuid.Slider] = 6,
        };

        private readonly List<GameControllerButtonInfo> buttonInfos = new List<GameControllerButtonInfo>();
        private readonly List<DirectInputAxisInfo> axisInfos = new List<DirectInputAxisInfo>();
        private readonly List<GameControllerDirectionInfo> directionInfos = new List<GameControllerDirectionInfo>();
        
        //private DirectInputGameController gamepad;
        private readonly DirectInputJoystick joystick;
        private DirectInputState state = new DirectInputState();

        public GameControllerDirectInput(InputSourceWindowsDirectInput source, DirectInput directInput, DeviceInstance instance)
        {
            Source = source;
            Name = instance.InstanceName.TrimEnd('\0');
            Id = instance.InstanceGuid;
            ProductId = instance.ProductGuid;
            joystick = new DirectInputJoystick(directInput, instance.InstanceGuid);
            joystick.SetCooperativeLevel(IntPtr.Zero, CooperativeLevel.NonExclusive | CooperativeLevel.Foreground);
            var objects = joystick.GetObjects();

            int sliderCount = 0;
            foreach (var obj in objects)
            {
                var objectId = obj.ObjectId;
                string objectName = obj.Name.TrimEnd('\0');
                
                GameControllerObjectInfo objectInfo = null;
                if (objectId.HasAnyFlag(DeviceObjectTypeFlags.Button | DeviceObjectTypeFlags.PushButton | DeviceObjectTypeFlags.ToggleButton))
                {
                    var buttonInfo = new GameControllerButtonInfo();
                    buttonInfo.Type = objectId.HasFlags(DeviceObjectTypeFlags.ToggleButton) ? GameControllerButtonType.ToggleButton : GameControllerButtonType.PushButton;
                    objectInfo = buttonInfo;
                    buttonInfos.Add(buttonInfo);
                }
                else if (objectId.HasAnyFlag(DeviceObjectTypeFlags.Axis | DeviceObjectTypeFlags.AbsoluteAxis | DeviceObjectTypeFlags.RelativeAxis))
                {
                    var axis = new DirectInputAxisInfo();
                    if (!GuidToAxisOffsets.TryGetValue(obj.ObjectType, out axis.Offset))
                    {
                        // Axis that should not be used, since it does not map to a valid object guid
                        continue;
                    }

                    // All objects after x/y/z and x/y/z rotation are sliders
                    if (obj.ObjectType == ObjectGuid.Slider)
                        axis.Offset += sliderCount++;
                    
                    objectInfo = axis;
                    axisInfos.Add(axis);
                }
                else if (objectId.HasFlags(DeviceObjectTypeFlags.PointOfViewController))
                {
                    var directionInfo = new GameControllerDirectionInfo();
                    objectInfo = directionInfo;
                    directionInfos.Add(directionInfo);
                }

                if (objectInfo != null)
                {
                    objectInfo.Name = objectName;
                }
            }
            
            // Sort axes, buttons and hats do not need to be sorted
            axisInfos.Sort((a, b) => a.Offset.CompareTo(b.Offset));

            InitializeButtonStates();
        }

        public void Dispose()
        {
            joystick.Dispose();
        }

        public override string Name { get; }

        public override Guid Id { get; }

        public override Guid ProductId { get; }

        public override IReadOnlyList<GameControllerButtonInfo> ButtonInfos => buttonInfos;

        public override IReadOnlyList<GameControllerAxisInfo> AxisInfos => axisInfos;

        public override IReadOnlyList<GameControllerDirectionInfo> DirectionInfos => directionInfos;

        public override IInputSource Source { get; }

        public event EventHandler Disconnected;
        
        /// <summary>
        /// Applies a deadzone to an axis input value
        /// </summary>
        /// <param name="value">The axis input value</param>
        /// <param name="deadZone">The deadzone treshold</param>
        /// <returns>The axis value with the applied deadzone</returns>
        public static float ClampDeadZone(float value, float deadZone)
        {
            if (value > 0.0f)
            {
                value -= deadZone;
                if (value < 0.0f)
                {
                    value = 0.0f;
                }
            }
            else
            {
                value += deadZone;
                if (value > 0.0f)
                {
                    value = 0.0f;
                }
            }

            // Renormalize the value according to the dead zone
            value = value / (1.0f - deadZone);
            return value < -1.0f ? -1.0f : value > 1.0f ? 1.0f : value;
        }

        public override void Update(List<InputEvent> inputEvents)
        {
            try
            {
                joystick.Acquire();
                joystick.Poll();
                joystick.GetCurrentState(ref state);
                
                // Some device might report (perhaps erroneously) more than 128 buttons, which DirectInputState doesn't support  
                // Should be investigated with such a device at hand for proper testing
                var buttonCount = buttonInfos.Count < state.Buttons.Length ? buttonInfos.Count : state.Buttons.Length;
                for (int i = 0; i < buttonCount; i++)
                {
                    HandleButton(i, state.Buttons[i]);
                }

                for (int i = 0; i < axisInfos.Count; i++)
                {
                    int axisMemIndex = axisInfos[i].Offset;
                    // See previous comment
                    if(axisMemIndex >= state.Axes.Length)
                        continue;
                    HandleAxis(i, ClampDeadZone(state.Axes[axisMemIndex] * 2.0f - 1.0f, InputManager.GameControllerAxisDeadZone));
                }
                
                // See previous comment
                var dirCount = state.PovControllers.Length < directionInfos.Count ? state.PovControllers.Length : directionInfos.Count;
                for (int i = 0; i < dirCount; i++)
                {
                    int povController = state.PovControllers[i];
                    HandleDirection(i, povController >= 0 ? Direction.FromTicks(povController, 36000) : Direction.None);
                }
            }
            catch (SharpDXException)
            {
                HandleDisconnect();
            }

            base.Update(inputEvents);
        }

        private void HandleDisconnect()
        {
            if (Disconnected == null)
                throw new InvalidOperationException("Something should handle controller disconnect");

            Disconnected.Invoke(this, null);
        }

        public class DirectInputAxisInfo : GameControllerAxisInfo
        {
            public int Offset;
        }
    }
}

#endif
