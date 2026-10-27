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

    /// <summary>
    /// Does nothing: the parent window owns the z-order and the activation of an embedded window.
    /// </summary>
    /// <remarks>
    /// The game raises its window after each device change, so on each resize of the host. Raising an SDL window also
    /// activates it (<c>SetForegroundWindow</c> on Windows, <c>_NET_ACTIVE_WINDOW</c> on X11), which took the focus from
    /// the main window and ended a resize of the main window by its edge after one step. The WPF editor embeds a WinForms
    /// child control, whose <c>BringToFront</c> only changes the z-order among its siblings.
    /// </remarks>
    public override void BringToFront()
    {
    }
}
