// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Games;

namespace Stride.Input.Tests;

/// <summary>
///   An <see cref="InputManager"/> driven by simulated devices, with no window and no graphics device.
/// </summary>
internal sealed class HeadlessInput : IDisposable
{
    private readonly GameTime gameTime = new();

    public HeadlessInput()
    {
        Input = new InputManager();
        Input.Initialize(new GameContextHeadless());

        var source = new InputSourceSimulated();
        Input.Sources.Add(source);
        Keyboard = source.AddKeyboard();
        Mouse = source.AddMouse();
    }

    public InputManager Input { get; }

    public KeyboardSimulated Keyboard { get; }

    public MouseSimulated Mouse { get; }

    /// <summary>
    ///   Runs one input frame: collects the simulated events and routes them to the listeners.
    /// </summary>
    public void Update() => Input.Update(gameTime);

    public void Dispose() => Input.Dispose();
}
