// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Xunit;

namespace Stride.Input.Tests;

/// <summary>
///   Covers how <see cref="InputManager"/> registers event listeners and routes events to them.
/// </summary>
public class TestInputListeners
{
    [Fact]
    public void ListenersAreInvokedInRegistrationOrder()
    {
        using var headless = new HeadlessInput();
        var received = new List<string>();
        var a = new RecordingListener("A", received);
        var b = new RecordingListener("B", received);
        var c = new RecordingListener("C", received);

        headless.Input.AddListener(a);
        headless.Input.AddListener(b);
        headless.Input.AddListener(c);
        headless.Input.RemoveListener(b);
        headless.Input.AddListener(b);
        headless.Keyboard.SimulateDown(Keys.Space);
        headless.Update();

        Assert.Equal(["A:Space", "C:Space", "B:Space"], received);
    }

    [Fact]
    public void AddingTheSameListenerTwiceDeliversEachEventOnce()
    {
        using var headless = new HeadlessInput();
        var received = new List<string>();
        var listener = new RecordingListener("A", received);

        headless.Input.AddListener(listener);
        headless.Input.AddListener(listener);
        headless.Keyboard.SimulateDown(Keys.Space);
        headless.Update();

        Assert.Equal(["A:Space"], received);
    }

    [Fact]
    public void ListenerRemovedDuringDispatchStillReceivesTheCurrentEventButNotTheNext()
    {
        using var headless = new HeadlessInput();
        var received = new List<string>();
        var remover = new RecordingListener("A", received);
        var removed = new RecordingListener("B", received);
        remover.OnEvent = _ => headless.Input.RemoveListener(removed);

        headless.Input.AddListener(remover);
        headless.Input.AddListener(removed);
        headless.Keyboard.SimulateDown(Keys.D1);
        headless.Keyboard.SimulateDown(Keys.D2);
        headless.Update();

        Assert.Equal(["A:D1", "B:D1", "A:D2"], received);
    }

    [Fact]
    public void ListenerAddedDuringDispatchReceivesTheNextEventButNotTheCurrent()
    {
        using var headless = new HeadlessInput();
        var received = new List<string>();
        var adder = new RecordingListener("A", received);
        var added = new RecordingListener("C", received);
        adder.OnEvent = _ => headless.Input.AddListener(added);

        headless.Input.AddListener(adder);
        headless.Keyboard.SimulateDown(Keys.D1);
        headless.Keyboard.SimulateDown(Keys.D2);
        headless.Update();

        Assert.Equal(["A:D1", "A:D2", "C:D2"], received);
    }

    private sealed class RecordingListener(string name, List<string> received) : IInputEventListener<KeyEvent>
    {
        public Action<KeyEvent> OnEvent { get; set; }

        public void ProcessEvent(KeyEvent inputEvent)
        {
            received.Add($"{name}:{inputEvent.Key}");
            OnEvent?.Invoke(inputEvent);
        }
    }
}
