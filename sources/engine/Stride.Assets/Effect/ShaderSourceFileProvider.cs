// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Enumeration;
using System.Linq;
using System.Text;
using Stride.Core.IO;
using Stride.Shaders.Compiler;

namespace Stride.Assets.Effect
{
    /// <summary>
    /// Serves the shader sources a <see cref="ShaderSourceLocator"/> finds under the URLs their import would write in the database,
    /// and passes every other request to the database.
    /// </summary>
    /// <remarks>
    /// For a shader file <c>X.sdsl</c>, it serves <c>shaders/X.sdsl</c> (the content of the file) and <c>shaders/X.sdsl/path</c>
    /// (its full path, as <see cref="Stride.Core.Assets.Compiler.ImportStreamCommand.SaveSourcePath"/> stores it), and
    /// <see cref="TryGetFileLocation"/> gives the file, which hot reload watches. The bytes are those of the file, so the hashes of
    /// the sources, and so every effect cache entry, stay the same as with an import.
    /// </remarks>
    public sealed class ShaderSourceFileProvider : VirtualFileProviderBase
    {
        private const string PathSuffix = "/path";
        private static readonly string ShaderFolder = EffectCompilerBase.DefaultSourceShaderFolder + VirtualFileSystem.DirectorySeparatorChar;

        private readonly ShaderSourceLocator locator;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderSourceFileProvider"/> class.
        /// </summary>
        /// <param name="locator">The locator of the shader source files.</param>
        /// <param name="database">The provider of everything else, usually the database the effects compile from.</param>
        public ShaderSourceFileProvider(ShaderSourceLocator locator, IVirtualFileProvider database)
            : base(null)
        {
            this.locator = locator ?? throw new ArgumentNullException(nameof(locator));
            Database = database ?? throw new ArgumentNullException(nameof(database));
        }

        /// <summary>
        /// Gets the provider of everything that is not a shader source.
        /// </summary>
        public IVirtualFileProvider Database { get; }

        /// <inheritdoc/>
        public override Stream OpenStream(string url, VirtualFileMode mode, VirtualFileAccess access, VirtualFileShare share = VirtualFileShare.Read, StreamFlags streamFlags = StreamFlags.None)
        {
            if (mode == VirtualFileMode.Open && access == VirtualFileAccess.Read && TryFindSource(url, out var filePath, out var isPathObject))
            {
                if (isPathObject)
                    return new MemoryStream(Encoding.UTF8.GetBytes(filePath.FullPath), false);

                // Another program, such as a text editor, can be writing the file
                return new FileStream(filePath.ToOSPath(), FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            }
            return Database.OpenStream(url, mode, access, share, streamFlags);
        }

        /// <inheritdoc/>
        public override bool FileExists(string url)
        {
            return TryFindSource(url, out _, out _) || Database.FileExists(url);
        }

        /// <inheritdoc/>
        public override long FileSize(string url)
        {
            if (TryFindSource(url, out var filePath, out var isPathObject))
                return isPathObject ? Encoding.UTF8.GetByteCount(filePath.FullPath) : new FileInfo(filePath.ToOSPath()).Length;
            return Database.FileSize(url);
        }

        /// <inheritdoc/>
        public override DateTime GetLastWriteTime(string url)
        {
            return TryFindSource(url, out var filePath, out _) ? File.GetLastWriteTime(filePath.ToOSPath()) : Database.GetLastWriteTime(url);
        }

        /// <inheritdoc/>
        public override bool TryGetFileLocation(string path, out string filePath, out long start, out long end)
        {
            if (TryFindSource(path, out var sourcePath, out var isPathObject) && !isPathObject)
            {
                filePath = sourcePath.ToOSPath();
                start = 0;
                end = -1;
                return true;
            }
            return Database.TryGetFileLocation(path, out filePath, out start, out end);
        }

        /// <inheritdoc/>
        public override string[] ListFiles(string url, string searchPattern, VirtualSearchOption searchOption)
        {
            var files = Database.ListFiles(url, searchPattern, searchOption);
            if (!string.Equals(url.TrimStart(VirtualFileSystem.DirectorySeparatorChar).TrimEnd(VirtualFileSystem.DirectorySeparatorChar), EffectCompilerBase.DefaultSourceShaderFolder, StringComparison.Ordinal))
                return files;

            var sources = locator.FileNames
                .Where(x => FileSystemName.MatchesSimpleExpression(searchPattern, x) && TryFindSource(ShaderFolder + x, out _, out _))
                .Select(x => ShaderFolder + x);
            return files.Union(sources, StringComparer.Ordinal).ToArray();
        }

        /// <inheritdoc/>
        public override bool DirectoryExists(string url) => Database.DirectoryExists(url);

        /// <inheritdoc/>
        public override void CreateDirectory(string url) => Database.CreateDirectory(url);

        /// <inheritdoc/>
        public override void FileDelete(string url) => Database.FileDelete(url);

        /// <inheritdoc/>
        public override void FileMove(string sourceUrl, string destinationUrl) => Database.FileMove(sourceUrl, destinationUrl);

        /// <inheritdoc/>
        public override void FileMove(string sourceUrl, IVirtualFileProvider destinationProvider, string destinationUrl) => Database.FileMove(sourceUrl, destinationProvider, destinationUrl);

        /// <inheritdoc/>
        public override string GetAbsolutePath(string path) => Database.GetAbsolutePath(path);

        /// <summary>
        /// Finds the source file a URL of the shader folder stands for.
        /// </summary>
        /// <param name="url">The URL, such as <c>shaders/X.sdsl</c> or <c>shaders/X.sdsl/path</c>.</param>
        /// <param name="filePath">The source file, which exists.</param>
        /// <param name="isPathObject">Whether the URL stands for the path of the file rather than its content.</param>
        private bool TryFindSource(string url, out UFile filePath, out bool isPathObject)
        {
            filePath = null!;
            isPathObject = false;

            var name = url.AsSpan().TrimStart(VirtualFileSystem.DirectorySeparatorChar);
            if (!name.StartsWith(ShaderFolder, StringComparison.Ordinal))
                return false;
            name = name[ShaderFolder.Length..];

            if (name.EndsWith(PathSuffix, StringComparison.Ordinal))
            {
                name = name[..^PathSuffix.Length];
                isPathObject = true;
            }

            // A file not saved yet, or deleted since the locator was updated, isn't there: the database may still have it
            return name.IndexOf(VirtualFileSystem.DirectorySeparatorChar) < 0
                && locator.TryGetFilePath(name.ToString(), out filePath)
                && File.Exists(filePath.ToOSPath());
        }
    }
}
