// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Avalonia.Controls;
using Stride.Games;

namespace Stride.Editor.Avalonia.Engine;

/// <summary>
/// A specialization of <see cref="GameFormSDL"/> that the editor embeds in a <see cref="GameEngineHost"/>.
/// </summary>
/// <remarks>
/// The WPF form forwards the keyboard and mouse messages of the hosted window to the host; the SDL window has no message
/// loop to hook, so the forwarding waits for the viewport work of the port (Phase 2).
/// </remarks>
public class EmbeddedGameForm : GameFormSDL
{
    public EmbeddedGameForm()
    {
        IsFullScreen = false;
    }

    /// <summary>
    /// Gets or sets the <see cref="GameEngineHost"/> associated to this form.
    /// </summary>
    public GameEngineHost? Host { get; set; }
}
