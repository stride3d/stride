// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.Controls;

namespace Stride.Core.Presentation.Avalonia.Extensions;

public static class WindowHelper
{
    /// <summary>
    /// Moves the <paramref cref="window"/> to the center of the given <paramref cref="area"/>.
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="area">The area in virtual screen coordinates.</param>
    public static void CenterToArea(this Window window, Rect area)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (area == default) return;

        var scaling = window.Screens.ScreenFromWindow(window) is { } screen ? screen.Scaling : 1.0;
        CenterToArea(window, area, scaling);
    }

    /// <summary>
    /// Moves the <paramref cref="window"/> to the center of the given <paramref cref="area"/>.
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="area">The area in virtual screen coordinates.</param>
    /// <param name="scaling">The scaling factor of the screen.</param>
    public static void CenterToArea(this Window window, Rect area, double scaling)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (area == default) return;

        var position = new Point(Math.Abs(area.Width - window.Width) / 2, Math.Abs(area.Height - window.Height) / 2) + area.Position;
        window.Position = PixelPoint.FromPoint(position, scaling);
    }

    /// <summary>
    /// Moves and resize the <paramref cref="window"/> to make it fill the whole given <paramref cref="area"/>.
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="area">The area in virtual screen coordinates.</param>
    public static void FillArea(this Window window, Rect area)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (area == default) return;

        var scaling = window.Screens.ScreenFromWindow(window) is { } screen ? screen.Scaling : 1.0;
        FillArea(window, area, scaling);
    }

    /// <summary>
    /// Moves and resize the <paramref cref="window"/> to make it fill the whole given <paramref cref="area"/>.
    /// </summary>
    /// <param name="window">The window.</param>
    /// <param name="area">The area in virtual screen coordinates.</param>
    /// <param name="scaling">The scaling factor of the screen.</param>
    public static void FillArea(this Window window, Rect area, double scaling)
    {
        ArgumentNullException.ThrowIfNull(window);
        if (area == default) return;

        window.Width = area.Width;
        window.Height = area.Height;
        window.Position = PixelPoint.FromPoint(area.Position, scaling);
    }
    
    /// <summary>
    /// Gets the available working area for this <paramref cref="window"/> on the screen.
    /// </summary>
    /// <param name="window">The window.</param>
    /// <returns>The available working area on the screen in virtual screen coordinates.</returns>
    public static (Rect area, double scaling) GetWorkingArea(this Window window)
    {
        if (window.Screens.ScreenFromWindow(window) is { } screen)
        {
            return (screen.WorkingArea.ToRect(screen.Scaling), screen.Scaling);
        }

        return (new Rect(0, 0, window.Width, window.Height), 1);
    }
}
