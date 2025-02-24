// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text.RegularExpressions;

using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Core.Storage;

namespace Stride.Core.IO;

public sealed class DatabaseFileProvider : VirtualFileProviderBase
{
    public DatabaseFileProvider(ObjectDatabase objectDatabase, string? mountPoint = null)
        : this(objectDatabase.ContentIndexMap, objectDatabase, mountPoint)
    {
    }

    public DatabaseFileProvider(IContentIndexMap contentIndexMap, ObjectDatabase objectDatabase, string? mountPoint = null)
        : base(mountPoint)
    {
        ContentIndexMap = contentIndexMap;
        ObjectDatabase = objectDatabase;
    }

    /// <summary>
    /// URL prefix for ObjectId references.
    /// </summary>
    public static readonly string ObjectIdUrl = "id://";

    public IContentIndexMap ContentIndexMap { get; }

    public ObjectDatabase ObjectDatabase { get; }

    /// <inheritdoc/>
    public override Stream OpenStream(string url, VirtualFileMode mode, VirtualFileAccess access, VirtualFileShare share = VirtualFileShare.Read, StreamFlags streamFlags = StreamFlags.None)
    {
        // Open or create the file through the underlying (IContentIndexMap, IOdbBackend) couple.
        // Also read/write a ObjectHeader.
        if (mode == VirtualFileMode.Open)
        {
            ObjectId objectId;
            if (url.StartsWith(ObjectIdUrl, StringComparison.Ordinal))
            {
                _ = ObjectId.TryParse(url[ObjectIdUrl.Length..], out objectId);
            }
            else if (!ContentIndexMap.TryGetValue(url, out objectId))
            {
                throw new FileNotFoundException($"Unable to find the file [{url}]");
            }

            var result = ObjectDatabase.OpenStream(objectId, mode, access, share);

            // copy the stream into a memory stream in order to make it seek-able
            if (streamFlags == StreamFlags.Seekable && !result.CanSeek)
            {
                var buffer = new byte[result.Length - result.Position];
                result.Read(buffer, 0, buffer.Length);
                return new DatabaseReadFileStream(objectId, new MemoryStream(buffer), 0);
            }

            return new DatabaseReadFileStream(objectId, result, result.Position);
        }

        if (mode == VirtualFileMode.Create)
        {
            if (url.StartsWith(ObjectIdUrl, StringComparison.Ordinal))
                throw new NotSupportedException();

            var stream = ObjectDatabase.CreateStream();

            // Header will be written by DatabaseWriteFileStream
            var result = new DatabaseWriteFileStream(stream, stream.Position);

            stream.Disposed += x =>
            {
                // Commit index changes
                ContentIndexMap[url] = x.CurrentHash;
            };

            return result;
        }

        throw new ArgumentException(null, nameof(mode));
    }

    /// <inheritdoc />
    /// <param name="url">The url (without preceding slash).</param>
    /// <param name="searchPattern"></param>
    /// <param name="searchOption"></param>
    /// <remarks>
    /// Example: to get all files within a directory <c>ListFiles("path/to/folder", "*", VirtualSearchOption.TopDirectoryOnly)</c>
    /// </remarks>
    public override string[] ListFiles(string url, string searchPattern, VirtualSearchOption searchOption)
    {
        var regex = CreateRegexForFileSearch(url, searchPattern, searchOption);

        return ContentIndexMap.SearchValues(x => regex.IsMatch(x.Key)).Select(x => x.Key).ToArray();
    }

    public override bool FileExists(string url)
    {
        return ContentIndexMap.TryGetValue(url, out var objectId)
               && ObjectDatabase.Exists(objectId);
    }

    public override long FileSize(string url)
    {
        if (!ContentIndexMap.TryGetValue(url, out var objectId))
            throw new FileNotFoundException();

        return ObjectDatabase.GetSize(objectId);
    }

    public override string GetAbsolutePath(string url)
    {
        if (!ContentIndexMap.TryGetValue(url, out var objectId))
            throw new FileNotFoundException();

        return ObjectDatabase.GetFilePath(objectId);
    }

    /// <summary>
    /// Resolves the given VFS URL into a ObjectId and its DatabaseFileProvider.
    /// </summary>
    /// <param name="url">The URL to resolve.</param>
    /// <param name="objectId">The object id.</param>
    /// <returns>The <see cref="DatabaseFileProvider"/> containing this object if it could be found; [null] otherwise.</returns>
    public static DatabaseFileProvider? ResolveObjectId(string url, out ObjectId objectId)
    {
        var resolveProviderResult = VirtualFileSystem.ResolveProvider(url, false);
        if (resolveProviderResult.Provider is not DatabaseFileProvider provider)
        {
            objectId = ObjectId.Empty;
            return null;
        }
        return provider.ContentIndexMap.TryGetValue(resolveProviderResult.Path, out objectId) ? provider : null;
    }

    public static Regex CreateRegexForFileSearch(string url, string searchPattern, VirtualSearchOption searchOption)
    {
        url = Regex.Escape(url);
        searchPattern = Regex.Escape(searchPattern).Replace(@"\*", "[^/]*").Replace(@"\?", "[^/]");
        string recursivePattern = searchOption == VirtualSearchOption.AllDirectories ? "(.*/)*" : "/?";
        return new Regex($"^{url}{recursivePattern}{searchPattern}$");
    }

    private abstract class DatabaseFileStream : VirtualFileStream, IDatabaseStream
    {
        protected DatabaseFileStream(Stream internalStream, long startPosition, bool seekToBeginning = true)
            : base(internalStream, startPosition, seekToBeginning: seekToBeginning)
        {
        }

        public abstract ObjectId ObjectId { get; }
    }

    private class DatabaseReadFileStream : DatabaseFileStream
    {
        public DatabaseReadFileStream(ObjectId id, Stream internalStream, long startPosition)
            : base(internalStream, startPosition, false)
        {
            ObjectId = id;
        }

        public override ObjectId ObjectId { get; }
    }

    private class DatabaseWriteFileStream : DatabaseFileStream
    {
        public DatabaseWriteFileStream(Stream internalStream, long startPosition)
            : base(internalStream, startPosition, false)
        {
        }

        public override ObjectId ObjectId
        {
            get
            {
                throw new NotSupportedException();
            }
        }
    }
}
