// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using Stride.Core.Collections;

namespace Stride.Input.Tests;

/// <summary>
///   A keyboard that implements <see cref="IKeyboardDevice"/> directly, the way a third-party device would,
///   without deriving from any engine base class.
/// </summary>
internal sealed class ThirdPartyKeyboard : IKeyboardDevice
{
    public readonly HashSet<Keys> Down = new();
    public readonly HashSet<Keys> Pressed = new();
    public readonly HashSet<Keys> Released = new();

    public ThirdPartyKeyboard()
    {
        DownKeys = new ReadOnlySet<Keys>(Down);
        PressedKeys = new ReadOnlySet<Keys>(Pressed);
        ReleasedKeys = new ReadOnlySet<Keys>(Released);
    }

    public string Name => "Third-party keyboard";

    public Guid Id { get; } = Guid.NewGuid();

    public int Priority { get; set; }

    public IInputSource Source => null;

    public Core.Collections.IReadOnlySet<Keys> PressedKeys { get; }

    public Core.Collections.IReadOnlySet<Keys> ReleasedKeys { get; }

    public Core.Collections.IReadOnlySet<Keys> DownKeys { get; }

    public void Update(List<InputEvent> inputEvents)
    {
    }
}
