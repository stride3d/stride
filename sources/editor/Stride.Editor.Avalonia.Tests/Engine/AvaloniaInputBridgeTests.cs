// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Stride.Core.Mathematics;
using Stride.Editor.Avalonia.Engine;
using Stride.Games;
using Stride.Input;
using MouseButton = Avalonia.Input.MouseButton;
using Point = Avalonia.Point;

namespace Stride.Editor.Avalonia.Tests.Engine;

public sealed class AvaloniaInputBridgeTests
{
    [AvaloniaFact]
    public void PointerWheelAndKeyEventsReachTheInputManager()
    {
        var view = new OffscreenGameView { Width = 100, Height = 100 };
        var window = new Window { Content = view, SizeToContent = SizeToContent.WidthAndHeight };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var bridge = new AvaloniaInputBridge(view);
        using var input = new InputManager();
        input.Initialize(new GameContextHeadless());
        var gameTime = new GameTime();

        window.MouseMove(new Point(25, 75));
        window.MouseDown(new Point(25, 75), MouseButton.Left);
        window.MouseWheel(new Point(25, 75), new Vector(0, 1));
        window.KeyPress(Key.A, RawInputModifiers.None, PhysicalKey.A, "a");
        bridge.Apply(input);
        input.Update(gameTime);

        // Positions are normalized to the view
        Assert.Equal(new Vector2(0.25f, 0.75f), input.MousePosition);
        Assert.True(input.IsMouseButtonDown(Stride.Input.MouseButton.Left));
        Assert.Equal(1, input.MouseWheelDelta);
        Assert.True(input.IsKeyDown(Keys.A));
        Assert.True(view.IsFocused);

        window.MouseUp(new Point(50, 75), MouseButton.Left);
        window.KeyRelease(Key.A, RawInputModifiers.None, PhysicalKey.A, "a");
        bridge.Apply(input);
        input.Update(gameTime);

        Assert.Equal(new Vector2(0.5f, 0.75f), input.MousePosition);
        Assert.False(input.IsMouseButtonDown(Stride.Input.MouseButton.Left));
        Assert.True(input.IsMouseButtonReleased(Stride.Input.MouseButton.Left));
        Assert.False(input.IsKeyDown(Keys.A));
        Assert.Equal(0, input.MouseWheelDelta);
    }

    [AvaloniaFact]
    public void ALockedMouseSeesTheMoveSinceThePreviousEvent()
    {
        var view = new OffscreenGameView { Width = 100, Height = 100 };
        var window = new Window { Content = view, SizeToContent = SizeToContent.WidthAndHeight };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        var bridge = new AvaloniaInputBridge(view);
        using var input = new InputManager();
        input.Initialize(new GameContextHeadless());
        var gameTime = new GameTime();

        window.MouseMove(new Point(20, 50));
        window.MouseDown(new Point(20, 50), MouseButton.Left);
        bridge.Apply(input);
        input.Update(gameTime);
        // The camera controls lock the mouse where the drag starts
        input.LockMousePosition();
        input.Update(gameTime);
        Assert.True(input.IsMousePositionLocked);

        // Two moves of 10 pixels: the game sees two deltas of a tenth of the view, not the distances from the lock point
        window.MouseMove(new Point(30, 50));
        window.MouseMove(new Point(40, 50));
        bridge.Apply(input);
        input.Update(gameTime);

        Assert.Equal(new Vector2(0.2f, 0), input.MouseDelta);
        Assert.Equal(new Vector2(0.2f, 0.5f), input.MousePosition);

        input.UnlockMousePosition();
        window.MouseMove(new Point(90, 50));
        bridge.Apply(input);
        input.Update(gameTime);

        Assert.Equal(new Vector2(0.9f, 0.5f), input.MousePosition);
    }

    [AvaloniaFact]
    public void ACtrlHeldBeforeTheViewHadTheFocusComesWithThePointer()
    {
        var view = new OffscreenGameView { Width = 100, Height = 100 };
        var other = new Button { Content = "Other" };
        var window = new Window { Content = new StackPanel { Children = { other, view } }, SizeToContent = SizeToContent.WidthAndHeight };
        window.Show();
        other.Focus();
        Dispatcher.UIThread.RunJobs();
        var bridge = new AvaloniaInputBridge(view);
        using var input = new InputManager();
        input.Initialize(new GameContextHeadless());
        var gameTime = new GameTime();
        var inView = view.TranslatePoint(new Point(50, 50), window)!.Value;

        // Ctrl goes down while the button has the focus, then the press in the view (Ctrl+drag from the hierarchy)
        window.KeyPress(Key.LeftCtrl, RawInputModifiers.Control, PhysicalKey.ControlLeft, null);
        window.MouseDown(inView, MouseButton.Left, RawInputModifiers.Control);
        bridge.Apply(input);
        input.Update(gameTime);

        Assert.True(input.IsKeyDown(Keys.LeftCtrl));

        // Released in the view, which has the focus now
        window.KeyRelease(Key.LeftCtrl, RawInputModifiers.None, PhysicalKey.ControlLeft, null);
        window.MouseMove(inView, RawInputModifiers.LeftMouseButton);
        bridge.Apply(input);
        input.Update(gameTime);

        Assert.False(input.IsKeyDown(Keys.LeftCtrl));
    }

    [AvaloniaFact]
    public void TheKeysHeldWhenTheViewLosesTheFocusAreReleased()
    {
        var view = new OffscreenGameView { Width = 100, Height = 100 };
        var other = new Button { Content = "Other" };
        var window = new Window { Content = new StackPanel { Children = { other, view } }, SizeToContent = SizeToContent.WidthAndHeight };
        window.Show();
        view.Focus();
        Dispatcher.UIThread.RunJobs();
        var bridge = new AvaloniaInputBridge(view);
        using var input = new InputManager();
        input.Initialize(new GameContextHeadless());
        var gameTime = new GameTime();

        window.KeyPress(Key.W, RawInputModifiers.None, PhysicalKey.W, "w");
        bridge.Apply(input);
        input.Update(gameTime);
        Assert.True(input.IsKeyDown(Keys.W));

        other.Focus();
        Dispatcher.UIThread.RunJobs();
        bridge.Apply(input);
        input.Update(gameTime);

        Assert.False(input.IsKeyDown(Keys.W));
    }
}
