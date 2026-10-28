// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.GameStudio.Avalonia.Desktop;

/// <summary>
/// The graphics API the editor's games (the asset preview, later the scene editor) render with: the one place that
/// decides it, so that an option like the WPF Game Studio's graphics API override is a small change later.
/// </summary>
/// <remarks>
/// The viewports share their frames with Avalonia's compositor (<c>GpuFramePresenter</c>), which imports Direct3D 11
/// textures on Windows (Avalonia renders with ANGLE on Direct3D 11) and Vulkan memory on Linux. So the editor renders
/// with Direct3D 11 on Windows whatever the project or the settings say: with Direct3D 12 or Vulkan, the frames would be
/// read back through the CPU. The other platforms only have Vulkan.
/// </remarks>
internal static class ViewportGraphicsApi
{
    private const string Direct3D11 = "Direct3D11";

    /// <summary>
    /// Gets the graphics API the editor loads, given the one the settings ask for.
    /// </summary>
    /// <param name="preference">The API of the settings, or <c>null</c> for the platform default.</param>
    /// <returns>The API to load, or <c>null</c> for the platform default.</returns>
    public static string? Resolve(string? preference)
    {
        return OperatingSystem.IsWindows() ? Direct3D11 : preference;
    }

    /// <summary>
    /// Gets whether <see cref="Resolve"/> ignores a preference: the editor renders with another API than the one asked for.
    /// </summary>
    public static bool Overrides(string? preference)
    {
        return preference != null && !string.Equals(Resolve(preference), preference, StringComparison.OrdinalIgnoreCase);
    }
}
