// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.VirtualReality;
using Xunit;

namespace Stride.Input.Tests;

/// <summary>
/// Covers what scripts and systems see of a captured VR hand controller.
/// </summary>
public class TestTouchControllerCapture
{
    [Fact]
    public void UncapturedControllerReportsTheBackendInput()
    {
        var (headless, controller) = Create();
        using var _ = headless;
        controller.Press(TouchControllerButton.A);
        controller.Touch(TouchControllerButton.Trigger);
        controller.TriggerValue = 0.8f;

        headless.Update();

        Assert.True(controller.IsPressedDown(TouchControllerButton.A));
        Assert.True(controller.IsPressed(TouchControllerButton.A));
        Assert.True(controller.IsTouched(TouchControllerButton.Trigger));
        Assert.Equal(0.8f, controller.Trigger);
    }

    [Fact]
    public void CapturedControllerReportsNoInputButKeepsItsPose()
    {
        var (headless, controller) = Create();
        using var _ = headless;
        controller.Press(TouchControllerButton.A);
        controller.Touch(TouchControllerButton.Trigger);
        controller.TriggerValue = 0.8f;
        controller.GripValue = 1f;
        controller.Thumbstick = new Vector2(0.5f, -0.5f);
        controller.Pose = new Vector3(1, 2, 3);
        headless.Update();

        headless.Input.TryCapture(controller, new object());

        Assert.False(controller.IsPressed(TouchControllerButton.A));
        Assert.False(controller.IsTouched(TouchControllerButton.Trigger));
        Assert.Equal(0f, controller.Trigger);
        Assert.Equal(0f, controller.Grip);
        Assert.Equal(Vector2.Zero, controller.ThumbstickAxis);
        Assert.Equal(new Vector3(1, 2, 3), controller.Position);
    }

    [Fact]
    public void CaptureBeginReportsHeldButtonsAsReleasedForOneFrame()
    {
        var (headless, controller) = Create();
        using var _ = headless;
        controller.Press(TouchControllerButton.Grip);
        headless.Update();
        controller.PressedDown.Clear();
        headless.Update();

        headless.Input.TryCapture(controller, new object());

        Assert.True(controller.IsPressReleased(TouchControllerButton.Grip));

        headless.Update();

        Assert.False(controller.IsPressReleased(TouchControllerButton.Grip));
    }

    [Fact]
    public void ButtonHeldThroughCaptureEndIsHiddenUntilPressedAgain()
    {
        var (headless, controller) = Create();
        using var _ = headless;
        var owner = new object();
        headless.Input.TryCapture(controller, owner);
        controller.Press(TouchControllerButton.A);
        headless.Update();
        controller.PressedDown.Clear();
        headless.Update();

        headless.Input.Release(controller, owner);

        Assert.False(controller.IsPressed(TouchControllerButton.A));

        controller.Pressed.Clear();
        controller.PressReleased.Add(TouchControllerButton.A);
        headless.Update();

        Assert.False(controller.IsPressReleased(TouchControllerButton.A));

        controller.PressReleased.Clear();
        controller.Press(TouchControllerButton.A);
        headless.Update();

        Assert.True(controller.IsPressedDown(TouchControllerButton.A));
        Assert.True(controller.IsPressed(TouchControllerButton.A));
    }

    [Fact]
    public void TurningMaskingOffShowsCapturedInput()
    {
        var (headless, controller) = Create();
        using var _ = headless;
        controller.Press(TouchControllerButton.A);
        controller.TriggerValue = 0.8f;
        headless.Input.MaskCapturedInput = false;
        headless.Update();

        headless.Input.TryCapture(controller, new object());

        Assert.True(controller.IsPressed(TouchControllerButton.A));
        Assert.Equal(0.8f, controller.Trigger);
    }

    [Fact]
    public void ControllerNotUpdatedByAnInputManagerReportsTheBackendInput()
    {
        var controller = new FakeTouchController();
        controller.Press(TouchControllerButton.A);

        Assert.True(controller.IsPressed(TouchControllerButton.A));
        Assert.True(controller.IsPressedDown(TouchControllerButton.A));
    }

    private static (HeadlessInput Headless, FakeTouchController Controller) Create()
    {
        var headless = new HeadlessInput();
        var source = new FakeVRSource();
        headless.Input.Sources.Add(source);
        var controller = new FakeTouchController();
        source.Add(controller);
        return (headless, controller);
    }

    private sealed class FakeVRSource : InputSourceBase
    {
        public override void Initialize(InputManager inputManager)
        {
        }

        public void Add(IInputDevice device) => RegisterDevice(device);
    }

    private sealed class FakeTouchController : TouchController
    {
        public HashSet<TouchControllerButton> Pressed { get; } = new();
        public HashSet<TouchControllerButton> PressedDown { get; } = new();
        public HashSet<TouchControllerButton> PressReleased { get; } = new();
        public HashSet<TouchControllerButton> Touched { get; } = new();
        public HashSet<TouchControllerButton> TouchedDown { get; } = new();
        public HashSet<TouchControllerButton> TouchReleased { get; } = new();

        public float TriggerValue { get; set; }
        public float GripValue { get; set; }
        public Vector2 Thumbstick { get; set; }
        public Vector3 Pose { get; set; }

        public void Press(TouchControllerButton button)
        {
            Pressed.Add(button);
            PressedDown.Add(button);
        }

        public void Touch(TouchControllerButton button)
        {
            Touched.Add(button);
            TouchedDown.Add(button);
        }

        public override Vector3 Position => Pose;
        public override Quaternion Rotation => Quaternion.Identity;
        public override Vector3 LinearVelocity => Vector3.Zero;
        public override Vector3 AngularVelocity => Vector3.Zero;
        public override DeviceState State => DeviceState.Valid;
        public override ControllerHaptics HapticsSupport => ControllerHaptics.None;

        protected override float RawTrigger => TriggerValue;
        protected override float RawGrip => GripValue;
        protected override bool RawIndexPointing => false;
        protected override bool RawIndexResting => false;
        protected override bool RawThumbUp => false;
        protected override bool RawThumbResting => false;
        protected override Vector2 RawThumbAxis => Thumbstick;
        protected override Vector2 RawThumbstickAxis => Thumbstick;

        protected override bool IsRawPressedDown(TouchControllerButton button) => PressedDown.Contains(button);
        protected override bool IsRawPressed(TouchControllerButton button) => Pressed.Contains(button);
        protected override bool IsRawPressReleased(TouchControllerButton button) => PressReleased.Contains(button);
        protected override bool IsRawTouchedDown(TouchControllerButton button) => TouchedDown.Contains(button);
        protected override bool IsRawTouched(TouchControllerButton button) => Touched.Contains(button);
        protected override bool IsRawTouchReleased(TouchControllerButton button) => TouchReleased.Contains(button);

        public override Task Vibrate(int durationMs, float frequency = 1, float amplitude = 1) => Task.CompletedTask;
    }
}
