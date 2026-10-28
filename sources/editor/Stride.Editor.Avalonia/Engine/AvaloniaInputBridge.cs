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
/// updates (<see cref="Apply"/>). Avalonia's <see cref="Key"/> has the values of Stride's <see cref="Keys"/> (both follow
/// WPF), so a key converts with a cast. The events are not marked handled: the editor's shortcuts keep working while the
/// viewport has the focus.
/// </para>
/// <para>
/// A locked mouse (the camera controls lock it while a button is down) measures each move from the lock position, as a
/// real locked cursor is warped back to it after each move. The editor's cursor keeps moving, so the bridge applies the
/// change since the previous event from the lock position.
/// </para>
/// </remarks>
internal sealed class AvaloniaInputBridge
{
    private readonly ConcurrentQueue<Action<MouseSimulated, KeyboardSimulated>> pending = new();
    private InputSourceSimulated? source;
    private MouseSimulated? mouse;
    private KeyboardSimulated? keyboard;
    private Vector2? lastPosition;

    /// <summary>
    /// Creates the bridge of a view (UI thread).
    /// </summary>
    public AvaloniaInputBridge(OffscreenGameView view)
    {
        view.PointerMoved += (_, e) => QueuePosition(view, e);
        view.PointerPressed += (_, e) =>
        {
            // The keyboard follows the pointer into the viewport
            view.Focus();
            QueuePosition(view, e);
            var button = ToMouseButton(e.GetCurrentPoint(view).Properties.PointerUpdateKind);
            pending.Enqueue((m, _) => m.SimulateMouseDown(button));
        };
        view.PointerReleased += (_, e) =>
        {
            QueuePosition(view, e);
            var button = ToMouseButton(e.GetCurrentPoint(view).Properties.PointerUpdateKind);
            pending.Enqueue((m, _) => m.SimulateMouseUp(button));
        };
        view.PointerWheelChanged += (_, e) =>
        {
            var delta = (float)e.Delta.Y;
            pending.Enqueue((m, _) => m.SimulateMouseWheel(delta));
        };
        view.KeyDown += (_, e) =>
        {
            var key = (Keys)(int)e.Key;
            pending.Enqueue((_, k) => k.SimulateDown(key));
        };
        view.KeyUp += (_, e) =>
        {
            var key = (Keys)(int)e.Key;
            pending.Enqueue((_, k) => k.SimulateUp(key));
        };
    }

    /// <summary>
    /// Applies the queued input to the game (game thread, before the update of its input manager).
    /// </summary>
    public void Apply(InputManager input)
    {
        if (source == null)
        {
            source = new InputSourceSimulated();
            input.Sources.Add(source);
            mouse = source.AddMouse();
            keyboard = source.AddKeyboard();
        }
        while (pending.TryDequeue(out var action))
            action(mouse!, keyboard!);
    }

    private void QueuePosition(OffscreenGameView view, PointerEventArgs e)
    {
        // Stride's mouse positions are normalized to the surface
        var point = e.GetPosition(view);
        var size = view.Bounds.Size;
        if (size.Width <= 0 || size.Height <= 0)
            return;
        var position = new Vector2((float)(point.X / size.Width), (float)(point.Y / size.Height));
        pending.Enqueue((m, _) => MoveTo(m, position));
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
}
