// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Concurrent;
using Avalonia.Input;
using Stride.Core.Mathematics;
using Stride.Input;
using MouseButton = Stride.Input.MouseButton;

namespace Stride.Editor.Avalonia.Engine;

/// <summary>
/// Feeds the keyboard and pointer input of an <see cref="OffscreenGameView"/> to a game that renders offscreen.
/// </summary>
/// <remarks>
/// <para>
/// A window-less game has no input source of its own. The bridge adds an <see cref="InputSourceSimulated"/> to the game:
/// the view queues its Avalonia events on the UI thread, and the game applies them on its own thread before its input
/// updates (<see cref="Apply"/>). Avalonia's <see cref="Key"/> has the values of Stride's <see cref="Keys"/>, so a key
/// converts with a cast. The events are not marked handled: the editor's shortcuts keep working while the
/// viewport has the focus.
/// </para>
/// <para>
/// A locked mouse (the camera controls lock it while a button is down) measures each move from the lock position, as a
/// real locked cursor is warped back to it after each move. The editor's cursor keeps moving, so the bridge applies the
/// change since the previous event from the lock position.
/// </para>
/// <para>
/// The keys reach the view only while it has the focus, which it takes on a press. A modifier (Ctrl, Shift, Alt) held
/// before, while another control had the focus, comes with the pointer events instead: the bridge presses it in the game,
/// and releases it when it is no longer held, so that Ctrl+drag works from a click in the hierarchy. The keys held when the
/// view loses the focus are released, as the view gets no release afterwards. So are the mouse buttons held when it loses
/// the focus or the capture of the pointer.
/// </para>
/// <para>
/// A click shorter than a frame of the game (a fast click on a slow renderer) would press and release the button in the
/// same update: the game would never see the button down, and the UI editor's adorners, which pick on the pressed and
/// released events of the frames that draw, would miss the click. The bridge keeps such a release, and the input queued
/// after it, for the next update, so the game sees the button down for one frame first.
/// </para>
/// </remarks>
internal sealed class AvaloniaInputBridge
{
    private readonly ConcurrentQueue<PendingInput> pending = new();
    private readonly HashSet<MouseButton> pressedThisUpdate = [];
    private readonly HashSet<MouseButton> droppedPresses = [];
    private PendingInput? held;
    private SimulatedInputDevices? ownDevices;
    private readonly HashSet<Keys> downKeys = [];
    private readonly HashSet<Keys> modifiersFromPointer = [];
    private readonly HashSet<MouseButton> downButtons = [];
    private Vector2? lastPosition;

    /// <summary>
    /// Creates the bridge of a view (UI thread).
    /// </summary>
    public AvaloniaInputBridge(OffscreenGameView view)
    {
        view.PointerMoved += (_, e) =>
        {
            SyncModifiers(e.KeyModifiers);
            QueuePosition(view, e);
        };
        view.PointerPressed += (_, e) =>
        {
            // The keyboard follows the pointer into the viewport
            view.Focus();
            SyncModifiers(e.KeyModifiers);
            QueuePosition(view, e);
            var button = ToMouseButton(e.GetCurrentPoint(view).Properties.PointerUpdateKind);
            downButtons.Add(button);
            pending.Enqueue(new PendingInput((m, _) => m.SimulateMouseDown(button)) { Pressed = button, IsPointer = true });
        };
        view.PointerReleased += (_, e) =>
        {
            QueuePosition(view, e);
            var button = ToMouseButton(e.GetCurrentPoint(view).Properties.PointerUpdateKind);
            if (downButtons.Remove(button))
                pending.Enqueue(Release(button));
        };
        // The view gets no button release once another element or window took the pointer
        view.PointerCaptureLost += (_, _) => ReleaseButtons();
        view.PointerWheelChanged += (_, e) =>
        {
            var delta = (float)e.Delta.Y;
            pending.Enqueue(new PendingInput((m, _) => m.SimulateMouseWheel(delta)) { IsPointer = true });
        };
        view.KeyDown += (_, e) =>
        {
            var key = (Keys)(int)e.Key;
            downKeys.Add(key);
            // A modifier pressed from a pointer event is already down
            if (!modifiersFromPointer.Remove(key))
                pending.Enqueue(new PendingInput((_, k) => k.SimulateDown(key)));
        };
        view.KeyUp += (_, e) =>
        {
            var key = (Keys)(int)e.Key;
            modifiersFromPointer.Remove(key);
            downKeys.Remove(key);
            pending.Enqueue(new PendingInput((_, k) => k.SimulateUp(key)));
        };
        view.LostFocus += (_, _) =>
        {
            // The view gets no key release once it lost the focus: the keys held are released now
            foreach (var key in downKeys)
            {
                pending.Enqueue(new PendingInput((_, k) => k.SimulateUp(key)));
            }
            downKeys.Clear();
            modifiersFromPointer.Clear();
            ReleaseButtons();
        };
    }

    private void ReleaseButtons()
    {
        foreach (var button in downButtons)
        {
            pending.Enqueue(Release(button));
        }
        downButtons.Clear();
    }

    /// <summary>
    /// Presses or releases the modifiers of a pointer event that the view didn't get as keys (UI thread).
    /// </summary>
    private void SyncModifiers(KeyModifiers modifiers)
    {
        SyncModifier(modifiers.HasFlag(KeyModifiers.Control), Keys.LeftCtrl, Keys.RightCtrl);
        SyncModifier(modifiers.HasFlag(KeyModifiers.Shift), Keys.LeftShift, Keys.RightShift);
        SyncModifier(modifiers.HasFlag(KeyModifiers.Alt), Keys.LeftAlt, Keys.RightAlt);
    }

    private void SyncModifier(bool isHeld, Keys left, Keys right)
    {
        if (isHeld && !downKeys.Contains(left) && !downKeys.Contains(right))
        {
            downKeys.Add(left);
            modifiersFromPointer.Add(left);
            pending.Enqueue(new PendingInput((_, k) => k.SimulateDown(left)));
        }
        else if (!isHeld && modifiersFromPointer.Remove(left))
        {
            downKeys.Remove(left);
            pending.Enqueue(new PendingInput((_, k) => k.SimulateUp(left)));
        }
    }

    /// <summary>
    /// Applies the queued input to the game (game thread, before the update of its input manager).
    /// </summary>
    public void Apply(InputManager input)
    {
        Apply(ownDevices ??= SimulatedInputDevices.Create(input), acceptPointer: true);
    }

    /// <summary>
    /// Applies the queued input to the game through devices shared by the bridges of several views of one game (game thread,
    /// before the update of its input manager).
    /// </summary>
    /// <param name="devices">The simulated mouse and keyboard of the game.</param>
    /// <param name="acceptPointer"><c>false</c> for a view that is not the active view of the game: its pointer moves, presses
    /// and wheel are dropped, as the game works in the active view; its keys and releases still reach the game.</param>
    public void Apply(SimulatedInputDevices devices, bool acceptPointer)
    {
        pressedThisUpdate.Clear();
        while (TakeNext() is { } next)
        {
            if (next.IsPointer && !acceptPointer)
            {
                if (next.Pressed is { } dropped)
                    droppedPresses.Add(dropped);
                continue;
            }
            // The release of a press that was dropped: the game never saw the button down
            if (next.Released is { } unseen && droppedPresses.Remove(unseen))
                continue;
            if (next.Released is { } released && pressedThisUpdate.Contains(released))
            {
                // A click shorter than a frame: the release and what follows wait for the next update
                held = next;
                break;
            }
            next.Apply(devices.Mouse, devices.Keyboard);
            if (next.Pressed is { } pressed)
                pressedThisUpdate.Add(pressed);
        }
    }

    private PendingInput? TakeNext()
    {
        if (held is { } next)
        {
            held = null;
            return next;
        }
        return pending.TryDequeue(out var queued) ? queued : null;
    }

    private static PendingInput Release(MouseButton button) => new((m, _) => m.SimulateMouseUp(button)) { Released = button };

    private void QueuePosition(OffscreenGameView view, PointerEventArgs e)
    {
        // Stride's mouse positions are normalized to the surface
        var point = e.GetPosition(view);
        var size = view.Bounds.Size;
        if (size.Width <= 0 || size.Height <= 0)
            return;
        var position = new Vector2((float)(point.X / size.Width), (float)(point.Y / size.Height));
        pending.Enqueue(new PendingInput((m, _) => MoveTo(m, position)) { IsPointer = true });
    }

    private void MoveTo(MouseSimulated mouse, Vector2 position)
    {
        if (mouse.IsPositionLocked && lastPosition is { } last)
            mouse.SetPosition(mouse.Position + (position - last));
        else
            mouse.SetPosition(position);
        lastPosition = position;
    }

    private static MouseButton ToMouseButton(PointerUpdateKind kind)
    {
        return kind switch
        {
            PointerUpdateKind.RightButtonPressed or PointerUpdateKind.RightButtonReleased => MouseButton.Right,
            PointerUpdateKind.MiddleButtonPressed or PointerUpdateKind.MiddleButtonReleased => MouseButton.Middle,
            PointerUpdateKind.XButton1Pressed or PointerUpdateKind.XButton1Released => MouseButton.Extended1,
            PointerUpdateKind.XButton2Pressed or PointerUpdateKind.XButton2Released => MouseButton.Extended2,
            _ => MouseButton.Left,
        };
    }

    /// <summary>
    /// An input queued on the UI thread for the game, and the mouse button it presses or releases, if any.
    /// </summary>
    private sealed record PendingInput(Action<MouseSimulated, KeyboardSimulated> Apply)
    {
        public MouseButton? Pressed { get; init; }

        public MouseButton? Released { get; init; }

        /// <summary>
        /// Gets whether this is a move, press or wheel of the pointer, which only the active view of a game applies.
        /// </summary>
        public bool IsPointer { get; init; }
    }
}

/// <summary>
/// The simulated mouse and keyboard through which the bridges of the views of one game give it their input.
/// </summary>
internal sealed class SimulatedInputDevices
{
    private SimulatedInputDevices(MouseSimulated mouse, KeyboardSimulated keyboard)
    {
        Mouse = mouse;
        Keyboard = keyboard;
    }

    /// <summary>
    /// Gets the simulated mouse.
    /// </summary>
    public MouseSimulated Mouse { get; }

    /// <summary>
    /// Gets the simulated keyboard.
    /// </summary>
    public KeyboardSimulated Keyboard { get; }

    /// <summary>
    /// Adds a simulated input source to the game, with a mouse and a keyboard (game thread).
    /// </summary>
    public static SimulatedInputDevices Create(InputManager input)
    {
        var source = new InputSourceSimulated();
        input.Sources.Add(source);
        return new SimulatedInputDevices(source.AddMouse(), source.AddKeyboard());
    }
}
