// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using Stride.Core.IO;
using Stride.Core.Serialization.Contents;

namespace Stride.Core.Storage;

/// <summary>
/// Object Database Backend (ODB) implementation using <see cref="VirtualFileSystem"/>
/// </summary>
public class FileOdbBackend : IOdbBackend
{
    private const int WriteBufferSize = 1024;
    private const string TempDirectory = "/tmp/";
    private const int ForcedMoveAttempts = 5;
    // Resolve provider once at initialization
    private readonly IVirtualFileProvider virtualFileProvider;
    private readonly string vfsRootUrl;
    private readonly string vfsTempUrl;
    private readonly ContentIndexMap contentIndexMap;

    public FileOdbBackend(string vfsRootUrl, string indexName, bool isReadOnly)
    {
        var resolveProviderResult = VirtualFileSystem.ResolveProvider(vfsRootUrl, true);
        virtualFileProvider = resolveProviderResult.Provider;
        this.vfsRootUrl = resolveProviderResult.Path;
        vfsTempUrl = this.vfsRootUrl + TempDirectory;

        // Ensure directories exists
        if (!virtualFileProvider.DirectoryExists(this.vfsRootUrl))
            virtualFileProvider.CreateDirectory(this.vfsRootUrl);

        IsReadOnly = isReadOnly;

        contentIndexMap = !string.IsNullOrEmpty(indexName) ? Serialization.Contents.ContentIndexMap.Load(vfsRootUrl + VirtualFileSystem.DirectorySeparatorChar + indexName, isReadOnly)
            : Serialization.Contents.ContentIndexMap.CreateInMemory();
        if (!isReadOnly && !virtualFileProvider.DirectoryExists(vfsTempUrl))
        {
            try
            {
                virtualFileProvider.CreateDirectory(vfsTempUrl);
            }
            catch (Exception)
            {
                IsReadOnly = true;
            }
        }
    }

    /// <inheritdoc/>
    public IContentIndexMap ContentIndexMap => contentIndexMap;

    public bool IsReadOnly { get; }

    public void Dispose()
    {
        contentIndexMap.Dispose();
    }

    /// <inheritdoc/>
    public virtual Stream OpenStream(ObjectId objectId, VirtualFileMode mode = VirtualFileMode.Open, VirtualFileAccess access = VirtualFileAccess.Read, VirtualFileShare share = VirtualFileShare.Read)
    {
        var url = BuildUrl(vfsRootUrl, objectId);

        // Try to early exit if file does not exists while opening, so that it doesn't
        // throw a file not found exception for default logic.
        if (!virtualFileProvider.FileExists(url))
        {
            if (mode == VirtualFileMode.Open || mode == VirtualFileMode.Truncate)
                throw new FileNotFoundException();

            // Otherwise, file creation is allowed, so make sure directory exists
            virtualFileProvider.CreateDirectory(ExtractPath(url));
        }

        return virtualFileProvider.OpenStream(url, mode, access, share);
    }

    /// <inheritdoc/>
    public virtual int GetSize(ObjectId objectId)
    {
        var url = BuildUrl(vfsRootUrl, objectId);
        using var file = virtualFileProvider.OpenStream(url, VirtualFileMode.Open, VirtualFileAccess.Read);
        return checked((int)file.Length);
    }

    /// <inheritdoc/>
    public virtual bool Exists(ObjectId objectId)
    {
        var url = BuildUrl(vfsRootUrl, objectId);
        return virtualFileProvider.FileExists(url);
    }

    /// <inheritdoc/>
    public virtual ObjectId Write(ObjectId objectId, Stream dataStream, int length, bool forceWrite = false)
    {
        if (objectId == ObjectId.Empty)
        {
            // This should be avoided
            using (var digestStream = new DigestStream(Stream.Null))
            {
                dataStream.CopyTo(digestStream);
                objectId = digestStream.CurrentHash;
            }

            dataStream.Seek(0, SeekOrigin.Begin);
        }

        string tmpFileName = vfsTempUrl + Guid.NewGuid() + ".tmp";

        var url = BuildUrl(vfsRootUrl, objectId);

        if (!forceWrite && virtualFileProvider.FileExists(url))
            return objectId;

        using (var file = virtualFileProvider.OpenStream(tmpFileName, VirtualFileMode.Create, VirtualFileAccess.Write))
        {
            dataStream.CopyTo(file);
        }

        MoveToDatabase(tmpFileName, objectId, forceWrite);

        return objectId;
    }

    /// <inheritdoc/>
    public OdbStreamWriter CreateStream()
    {
        if (IsReadOnly)
            throw new InvalidOperationException("Read-only backend.");

        string tmpFileName = vfsTempUrl + Guid.NewGuid() + ".tmp";
        Stream stream = virtualFileProvider.OpenStream(tmpFileName, VirtualFileMode.Create, VirtualFileAccess.Write);
        return new DigestStream(stream, tmpFileName) { Disposed = x => MoveToDatabase(x.TemporaryName, x.CurrentHash) };
    }

    private void MoveToDatabase(string temporaryFilePath, ObjectId objId, bool forceWrite = false)
    {
        string fileUrl = BuildUrl(vfsRootUrl, objId);

        // Concurrent writers of the same object, in this process or another, are settled by the move below
        var fileExists = virtualFileProvider.FileExists(fileUrl);

        // File may already exists, in this case we decide to not override it.
        if (fileExists && !forceWrite)
        {
            // But we should still clean our temporary file
            virtualFileProvider.FileDelete(temporaryFilePath);
            return;
        }

        if (!fileExists)
        {
            // Remove the second part of ObjectId to get the path (cf BuildUrl)
            virtualFileProvider.CreateDirectory(fileUrl[..^30]);
        }

        for (var attempt = 1; ; attempt++)
        {
            try
            {
                // A forced write replaces the file in one step: its content isn't always derived from its id, so readers must always find one
                virtualFileProvider.FileMove(temporaryFilePath, fileUrl, overwrite: forceWrite);
                return;
            }
            catch (Exception e) when (e.GetType() == typeof(IOException) || (forceWrite && e is UnauthorizedAccessException))
            {
                // Ignore only IOException "The destination file already exists." (another writer stored it first): subclasses are real errors.
                // A forced write gets "access denied" on Windows while anyone has the destination open: retry, then keep their content.
                if (forceWrite && attempt < ForcedMoveAttempts)
                {
                    Thread.Sleep(attempt - 1);
                    continue;
                }

                // But we should still clean our temporary file
                virtualFileProvider.FileDelete(temporaryFilePath);
                return;
            }
        }
    }

    /// <inheritdoc/>
    public void Delete(ObjectId objectId)
    {
        var url = BuildUrl(vfsRootUrl, objectId);
        virtualFileProvider.FileDelete(url);
    }

    /// <inheritdoc/>
    public IEnumerable<ObjectId> EnumerateObjects()
    {
        foreach (var file in virtualFileProvider.ListFiles(vfsRootUrl, "*", VirtualSearchOption.AllDirectories))
        {
            if (file.Length >= 2 + ObjectId.HashStringLength)
            {
                if (file[file.Length - ObjectId.HashStringLength - 2] != VirtualFileSystem.DirectorySeparatorChar
                    || file[file.Length - ObjectId.HashStringLength + 1] != VirtualFileSystem.DirectorySeparatorChar)
                    continue;

                var objectIdString = new char[ObjectId.HashStringLength];
                var filePosition = file.Length - ObjectId.HashStringLength - 1;
                for (int i = 0; i < ObjectId.HashStringLength; ++i)
                {
                    // Skip /
                    if (i == 2)
                        filePosition++;
                    objectIdString[i] = file[filePosition++];
                }

                if (ObjectId.TryParse(new string(objectIdString), out var objectId))
                    yield return objectId;
            }
        }
    }

    public string GetFilePath(ObjectId objectId)
    {
        return virtualFileProvider.GetAbsolutePath(BuildUrl(vfsRootUrl, objectId));
    }

    /// <summary>
    /// Touch throttle (X): a stored object's mtime is only refreshed by <see cref="Touch"/> if it is
    /// already older than this, bounding write amplification. GC eviction age must stay above it.
    /// </summary>
    public static TimeSpan TouchThrottle { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Refreshes an object's last-write time so mtime-LRU GC treats it as recently used. Best-effort
    /// and throttled by <see cref="TouchThrottle"/>; no-op on read-only backends.
    /// </summary>
    public void Touch(ObjectId objectId)
    {
        if (IsReadOnly)
            return;

        try
        {
            var info = new FileInfo(GetFilePath(objectId));
            if (info.Exists && DateTime.UtcNow - info.LastWriteTimeUtc > TouchThrottle)
                File.SetLastWriteTimeUtc(info.FullName, DateTime.UtcNow);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static string ExtractPath(string url)
    {
        return url[..url.LastIndexOf('/')];
    }

    public static string BuildUrl(string vfsRootUrl, ObjectId objectId)
    {
        var id = objectId.ToString();
        var result = new StringBuilder(vfsRootUrl.Length + 2 + ObjectId.HashStringLength);
        result.Append(vfsRootUrl);
        result.Append('/');
        result.Append(id[0]);
        result.Append(id[1]);
        result.Append('/');
        result.Append(id, 2, ObjectId.HashStringLength - 2);

        return result.ToString();
    }
}
