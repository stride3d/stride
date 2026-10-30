// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;

// Shared by source between the launcher and the CLI.
namespace Stride;

/// <summary>
///   A Game Studio. Each one ships in a package of its own, named like its assembly, and a Stride version can have
///   one or both installed.
/// </summary>
public enum GameStudioEditor
{
    /// <summary>The WPF Game Studio (Stride.GameStudio), which runs on Windows only, or under Wine.</summary>
    Wpf,

    /// <summary>The cross-platform Avalonia Game Studio (Stride.GameStudio.Avalonia.Desktop).</summary>
    Avalonia,
}

/// <summary>The packages and names of the <see cref="GameStudioEditor"/> values.</summary>
public static class GameStudioEditors
{
    /// <summary>The package of the WPF Game Studio, which is also the name of its assembly.</summary>
    public const string WpfPackageId = "Stride.GameStudio";

    /// <summary>The package of the Avalonia Game Studio, which is also the name of its assembly.</summary>
    public const string AvaloniaPackageId = "Stride.GameStudio.Avalonia.Desktop";

    /// <summary>Every Game Studio, in the order they are listed.</summary>
    public static IReadOnlyList<GameStudioEditor> All { get; } = [GameStudioEditor.Wpf, GameStudioEditor.Avalonia];

    /// <summary>The package of <paramref name="editor"/>, which is also the name of its assembly.</summary>
    public static string PackageId(this GameStudioEditor editor) => editor switch
    {
        GameStudioEditor.Wpf => WpfPackageId,
        GameStudioEditor.Avalonia => AvaloniaPackageId,
        _ => throw new ArgumentOutOfRangeException(nameof(editor), editor, null),
    };

    /// <summary>The name users know <paramref name="editor"/> by: WPF or Avalonia.</summary>
    public static string DisplayName(this GameStudioEditor editor) => editor switch
    {
        GameStudioEditor.Wpf => "WPF",
        GameStudioEditor.Avalonia => "Avalonia",
        _ => throw new ArgumentOutOfRangeException(nameof(editor), editor, null),
    };

    /// <summary>The Game Studio the package <paramref name="packageId"/> ships, or null when it ships none.</summary>
    public static GameStudioEditor? FromPackageId(string? packageId)
        => All.Where(editor => string.Equals(editor.PackageId(), packageId, StringComparison.OrdinalIgnoreCase))
            .Select(editor => (GameStudioEditor?)editor)
            .FirstOrDefault();

    /// <summary>Whether <paramref name="editor"/> runs natively on this OS: the WPF one needs Windows, or Wine.</summary>
    public static bool RunsNatively(this GameStudioEditor editor)
        => editor != GameStudioEditor.Wpf || OperatingSystem.IsWindows();
}
