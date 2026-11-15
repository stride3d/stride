// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Dock.Model.Core;
using Dock.Model.Inpc;
using Stride.GameStudio.Avalonia.Views;

namespace Stride.GameStudio.Avalonia.Docking;

/// <summary>
/// Dock's factory of the main window: the floating windows are <see cref="StrideHostWindow"/>s, which keep the shortcuts of
/// the main window.
/// </summary>
public sealed class StrideDockFactory : Factory
{
    /// <summary>
    /// The main window, which the floating windows take their shortcuts from; set by the main view.
    /// </summary>
    public MainWindow? MainWindow { get; set; }

    /// <inheritdoc />
    public override void InitLayout(IDockable layout)
    {
        DockableLocator ??= new Dictionary<string, Func<IDockable?>>();
        HostWindowLocator = new Dictionary<string, Func<IHostWindow?>>
        {
            [nameof(IDockWindow)] = () => new StrideHostWindow(MainWindow),
        };
        DefaultHostWindowLocator = () => new StrideHostWindow(MainWindow);
        base.InitLayout(layout);
    }
}
