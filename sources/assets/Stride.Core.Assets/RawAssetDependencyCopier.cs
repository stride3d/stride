// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;
using Stride.Core.IO;

namespace Stride.Core.Assets;

/// <summary>
/// Copies the files a raw asset needs, such as the textures of a model, along with a raw asset copied into a project.
/// </summary>
public static class RawAssetDependencyCopier
{
    /// <summary>
    /// Copies the files that <paramref name="importers"/> read for <paramref name="originalFile"/> to where they read them
    /// relative to <paramref name="copiedFile"/>.
    /// </summary>
    /// <remarks>
    /// Files that already exist next to the copy are left alone. A file that would go outside the copy's folder is not copied,
    /// and a warning is logged: the importer reports it as missing when the asset is imported.
    /// </remarks>
    /// <param name="originalFile">The raw asset that was copied.</param>
    /// <param name="copiedFile">The copy.</param>
    /// <param name="importers">The importers for the raw asset, which report the files it needs.</param>
    /// <param name="logger">The logger for messages.</param>
    /// <returns>The files that were copied.</returns>
    public static IReadOnlyList<UFile> CopyDependencies(UFile originalFile, UFile copiedFile, IEnumerable<IAssetImporter> importers, Logger logger)
    {
        var copiedDirectory = Path.GetFullPath(copiedFile.GetFullDirectory().ToOSPath());
        var copied = new List<UFile>();

        foreach (var importer in importers)
        {
            foreach (var dependency in importer.GetSourceDependencies(originalFile, logger))
            {
                var target = Path.GetFullPath(Path.Combine(copiedDirectory, dependency.RelativePath));
                if (!target.StartsWith(copiedDirectory + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    logger.Warning($"'{dependency.Source.GetFileName()}', which '{originalFile.GetFileName()}' needs, is outside its folder ('{dependency.RelativePath}'), so it was not copied.");
                    continue;
                }

                if (File.Exists(target))
                    continue;

                Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                File.Copy(dependency.Source.ToOSPath(), target);
                copied.Add(new UFile(target));
            }
        }

        return copied;
    }
}
