// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Cli.Core;

/// <summary>
///   A Game Studio. Each one ships in a package of its own, named like its assembly, and a Stride version can have
///   one or both installed.
/// </summary>
public enum GameStudioEditor
{
    /// <summary>The WPF Game Studio (Stride.GameStudio), which runs on Windows only.</summary>
    Wpf,

    /// <summary>The cross-platform Avalonia Game Studio (Stride.GameStudio.Avalonia.Desktop).</summary>
    Avalonia,
}

/// <summary>The packages and names of the <see cref="GameStudioEditor"/> values.</summary>
public static class GameStudioEditors
{
    /// <summary>Every Game Studio, in the order they are listed.</summary>
    public static IReadOnlyList<GameStudioEditor> All { get; } = [GameStudioEditor.Wpf, GameStudioEditor.Avalonia];

    /// <summary>The package of <paramref name="editor"/>, which is also the name of its assembly.</summary>
    public static string PackageId(this GameStudioEditor editor) => editor switch
    {
        GameStudioEditor.Wpf => "Stride.GameStudio",
        GameStudioEditor.Avalonia => "Stride.GameStudio.Avalonia.Desktop",
        _ => throw new ArgumentOutOfRangeException(nameof(editor), editor, null),
    };

    /// <summary>The name users know <paramref name="editor"/> by, as the launcher shows it.</summary>
    public static string DisplayName(this GameStudioEditor editor) => editor switch
    {
        GameStudioEditor.Wpf => "WPF",
        GameStudioEditor.Avalonia => "Avalonia",
        _ => throw new ArgumentOutOfRangeException(nameof(editor), editor, null),
    };

    /// <summary>The Game Studio the package <paramref name="packageId"/> ships, or null when it ships none.</summary>
    public static GameStudioEditor? FromPackageId(string packageId)
        => All.Where(editor => string.Equals(editor.PackageId(), packageId, StringComparison.OrdinalIgnoreCase))
            .Select(editor => (GameStudioEditor?)editor)
            .FirstOrDefault();

    /// <summary>Whether <paramref name="editor"/> runs natively on this OS: the WPF one needs Windows, or Wine.</summary>
    public static bool RunsNatively(this GameStudioEditor editor)
        => editor != GameStudioEditor.Wpf || OperatingSystem.IsWindows();

    /// <summary>
    ///   The Game Studio to open when none is asked for, among the <paramref name="installed"/> ones: on Windows the
    ///   WPF one when it is there, else the Avalonia one; elsewhere the Avalonia one, else the WPF one under Wine when
    ///   <paramref name="wine"/> is available. Null when none of them can run.
    /// </summary>
    public static GameStudioEditor? Default(IEnumerable<GameStudioEditor> installed, bool wine)
    {
        GameStudioEditor[] preferred = OperatingSystem.IsWindows()
            ? [GameStudioEditor.Wpf, GameStudioEditor.Avalonia]
            : [GameStudioEditor.Avalonia, GameStudioEditor.Wpf];
        var candidates = installed.ToList();
        return preferred
            .Where(editor => candidates.Contains(editor) && (editor.RunsNatively() || wine))
            .Select(editor => (GameStudioEditor?)editor)
            .FirstOrDefault();
    }
}
