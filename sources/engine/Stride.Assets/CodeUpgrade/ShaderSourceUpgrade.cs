// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Stride.Core.Assets;
using Stride.Core.Diagnostics;
using Stride.Core.IO;
using Stride.Shaders.Parsing.Upgrade;

namespace Stride.Assets;

/// <summary>
/// Upgrades the <c>.sdsl</c> and <c>.sdfx</c> files of a project to the current shader language (<see cref="ShaderUpgrader"/>).
/// </summary>
internal static class ShaderSourceUpgrade
{
    /// <summary>The step that writes <c>effect</c> in place of <c>shader</c> on an effect of an <c>.sdfx</c> file.</summary>
    public const string EffectKeywordsStep = "effect-keywords";

    // Two paths name the same file whatever their case where the file system ignores it, as Path.GetRelativePath reads them
    private static readonly StringComparer PathComparer = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    /// <summary>
    /// The folders that hold the files of a package: the folder of its project, and its asset folders outside of it.
    /// </summary>
    public static IReadOnlyList<string> Folders(UFile projectFullPath, Package package)
    {
        var projectFolder = Path.GetFullPath(projectFullPath.GetFullDirectory().ToOSPath());
        var folders = new List<string> { projectFolder };
        if (package.RootDirectory is { } root)
        {
            folders.AddRange(package.AssetFolders.Select(x => Path.GetFullPath(UPath.Combine(root, x.Path).ToOSPath()))
                .Where(x => Directory.Exists(x) && !IsInside(projectFolder, x)));
        }
        return folders.Distinct(PathComparer).ToList();
    }

    private static bool IsInside(string folder, string path)
    {
        var relative = Path.GetRelativePath(folder, path);
        return !Path.IsPathRooted(relative) && relative != ".." && !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal);
    }

    /// <param name="folders">The folders of the project (<see cref="Folders"/>); their shader files are upgraded (<c>bin</c>, <c>obj</c> and the backups of earlier upgrades left out).</param>
    /// <param name="steps">The steps of the gate (<c>ShaderUpgrader.Steps</c>, <see cref="EffectKeywordsStep"/>): only those run.</param>
    /// <param name="backup">The copy-on-write backup to snapshot each file into before writing it, or <c>null</c>.</param>
    /// <returns><c>false</c> when a file couldn't be read, and nothing was written, or couldn't be written.</returns>
    public static bool Run(IReadOnlyList<string> folders, IReadOnlyCollection<string> steps, UpgradeBackup backup, ILogger log)
    {
        // Every file is read before any is written: the steps decide across files
        var effects = steps.Contains(EffectKeywordsStep) ? Read(folders, "*.sdfx", log) : [];
        var sources = steps.Any(ShaderUpgrader.Steps.Contains) ? Read(folders, "*.sdsl", log) : [];
        if (effects == null || sources == null)
            return false;

        var writes = new List<(ShaderFile File, string Text, string Message)>();
        foreach (var effect in effects)
        {
            try
            {
                if (StridePackageUpgrader.UpgradeEffectShaderKeywords(effect.Text, out var count) is { } upgraded)
                    writes.Add((effect, upgraded, $"[{effect.Name}]: {count} effect(s) declared with 'shader' are now declared with 'effect'."));
            }
            catch (Exception e)
            {
                log.Warning($"Could not upgrade the effect file [{effect.Name}]", e);
            }
        }

        IReadOnlyList<string> notes = [];
        if (sources.Count > 0)
        {
            // By name, so that the notes of the upgrade name the files as the messages do
            var result = ShaderUpgrader.Upgrade(sources.ToDictionary(x => x.Name, x => x.Text, StringComparer.Ordinal), EngineShaderClasses.Current, steps: steps);
            var byName = sources.ToDictionary(x => x.Name, StringComparer.Ordinal);
            foreach (var (name, text) in result.Files)
            {
                if (text != byName[name].Text)
                    writes.Add((byName[name], text, $"Upgraded shader [{name}] to the current shader language"));
            }
            notes = result.Notes;
        }

        foreach (var (file, text, message) in writes)
        {
            try
            {
                backup?.Snapshot(file.FullPath);
                File.WriteAllText(file.FullPath, text, new UTF8Encoding(file.HasBom));
                log.Info(message);
            }
            catch (Exception e)
            {
                log.Error($"Unable to write the upgraded shader [{file.Name}]", e);
                return false;
            }
        }
        foreach (var note in notes)
            log.Warning($"Shader upgrade: {note}");
        return true;
    }

    // A shader file of the project as read: its name is the one the messages give it
    private sealed record ShaderFile(string FullPath, string Name, string Text, bool HasBom);

    // The shader files of the project, or null when one can't be read
    private static List<ShaderFile> Read(IReadOnlyList<string> folders, string pattern, ILogger log)
    {
        var files = new List<ShaderFile>();
        string current = null;
        try
        {
            foreach (var path in ProjectFiles(folders, pattern))
            {
                current = path;
                var bytes = File.ReadAllBytes(path);
                var hasBom = bytes is [0xEF, 0xBB, 0xBF, ..];
                var text = new UTF8Encoding(hasBom).GetString(bytes, hasBom ? 3 : 0, bytes.Length - (hasBom ? 3 : 0));
                files.Add(new ShaderFile(path, Name(folders, path), text, hasBom));
                current = null;
            }
        }
        catch (Exception e)
        {
            log.Error(current != null ? $"Unable to read the shader [{Name(folders, current)}]" : $"Unable to list the {pattern} files of the project", e);
            return null;
        }
        return files;
    }

    private static IEnumerable<string> ProjectFiles(IReadOnlyList<string> folders, string pattern)
    {
        return folders.SelectMany(folder => Directory.EnumerateFiles(folder, pattern, SearchOption.AllDirectories).Where(file =>
        {
            var parts = Path.GetRelativePath(folder, file).Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return !parts.Any(x => x.Equals("bin", StringComparison.OrdinalIgnoreCase) || x.Equals("obj", StringComparison.OrdinalIgnoreCase)
                || x.StartsWith(".stride-backup", StringComparison.OrdinalIgnoreCase));
        })).Select(Path.GetFullPath).Distinct(PathComparer);
    }

    // A file as the messages name it: from the folder of the project
    private static string Name(IReadOnlyList<string> folders, string file) => Path.GetRelativePath(folders[0], file).Replace('\\', '/');
}
