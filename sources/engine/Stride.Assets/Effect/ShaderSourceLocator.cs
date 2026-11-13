// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Stride.Core;
using Stride.Core.Assets;
using Stride.Core.Assets.Compiler;
using Stride.Core.IO;

namespace Stride.Assets.Effect
{
    /// <summary>
    /// Finds the source file of each shader of a set of packages by its file name, the name the import of an <see cref="EffectShaderAsset"/>
    /// gives it in the database (<c>shaders/X.sdsl</c>), so that a compiler can read the files where they are instead of importing them.
    /// </summary>
    /// <remarks>
    /// Two shaders with the same file name follow the rule of the import: the last one, in package order then asset order, wins.
    /// <see cref="Update"/> and the lookups can run on different threads.
    /// </remarks>
    public sealed class ShaderSourceLocator
    {
        /// <summary>
        /// The key of the locator of a compilation, in <see cref="AssetCompilerContext.Properties"/>.
        /// </summary>
        public static readonly PropertyKey<ShaderSourceLocator> Key = new PropertyKey<ShaderSourceLocator>("ShaderSourceLocator", typeof(ShaderSourceLocator));

        private volatile Snapshot snapshot = new Snapshot(new Dictionary<string, UFile>(), []);

        /// <summary>
        /// Gets the file names of the shaders, such as <c>X.sdsl</c>.
        /// </summary>
        public IReadOnlyCollection<string> FileNames => snapshot.Files.Keys;

        /// <summary>
        /// Gets the name and version of each package that has shaders, in package order.
        /// </summary>
        public IReadOnlyList<(string Name, string? Version)> Packages => snapshot.Packages;

        /// <summary>
        /// Creates a locator of the shaders of the given packages.
        /// </summary>
        /// <param name="packages">The packages, in the order of the import.</param>
        public static ShaderSourceLocator Create(IEnumerable<Package> packages)
        {
            var locator = new ShaderSourceLocator();
            locator.Update(packages);
            return locator;
        }

        /// <summary>
        /// Gets the locator of a compilation, creating it from the packages of the session the first time.
        /// </summary>
        /// <param name="context">The context of the compilation; Game Studio puts the locator of its session there.</param>
        /// <param name="session">The session whose packages to read when the context has no locator yet.</param>
        public static ShaderSourceLocator GetOrCreate(AssetCompilerContext context, PackageSession session)
        {
            ArgumentNullException.ThrowIfNull(context);
            ArgumentNullException.ThrowIfNull(session);
            return (ShaderSourceLocator)context.Properties.GetOrAdd(Key, _ => Create(session.Packages));
        }

        /// <summary>
        /// Finds the source file of a shader.
        /// </summary>
        /// <param name="fileName">The file name of the shader, such as <c>X.sdsl</c>.</param>
        /// <param name="filePath">The full path of its source file.</param>
        /// <returns><c>true</c> if a package has a shader with this file name; otherwise, <c>false</c>.</returns>
        public bool TryGetFilePath(string fileName, [MaybeNullWhen(false)] out UFile filePath)
        {
            return snapshot.Files.TryGetValue(fileName, out filePath);
        }

        /// <summary>
        /// Reads the shaders of the packages again.
        /// </summary>
        /// <param name="packages">The packages, in the order of the import.</param>
        /// <returns>The file names of the shaders that were added or removed, or now point to another file.</returns>
        public IReadOnlyCollection<string> Update(IEnumerable<Package> packages)
        {
            ArgumentNullException.ThrowIfNull(packages);

            var files = new Dictionary<string, UFile>();
            var packagesWithShaders = new List<(string Name, string? Version)>();
            foreach (var package in packages)
            {
                var hasShaders = false;
                foreach (var assetItem in package.Assets)
                {
                    if (assetItem.Asset is not EffectShaderAsset)
                        continue;

                    // The URL of the import (EffectShaderAssetCompiler) is the file name: the last one wins
                    var filePath = assetItem.FullPath;
                    files[Path.GetFileName(filePath.FullPath)] = filePath;
                    hasShaders = true;
                }
                if (hasShaders)
                    packagesWithShaders.Add((package.Meta.Name ?? string.Empty, package.Meta.Version?.ToString()));
            }

            var previous = snapshot.Files;
            snapshot = new Snapshot(files, packagesWithShaders);

            return previous.Keys.Union(files.Keys)
                .Where(x => !(previous.TryGetValue(x, out var before) && files.TryGetValue(x, out var after) && before == after))
                .ToList();
        }

        private sealed record Snapshot(Dictionary<string, UFile> Files, List<(string Name, string? Version)> Packages);
    }
}
