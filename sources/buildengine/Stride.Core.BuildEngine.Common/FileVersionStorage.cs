// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using Stride.Core.IO;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Serializers;
using Stride.Core.Storage;

namespace Stride.Core.BuildEngine;

/// <summary>
/// Storage used for <see cref="FileVersionKey"/> associated with an <see cref="ObjectId"/>.
/// </summary>
[DataSerializerGlobal(typeof(KeyValuePairSerializer<FileVersionKey, ObjectId>))]
public sealed class FileVersionStorage : DictionaryStore<FileVersionKey, ObjectId>
{
    private static readonly TimeSpan PruneInterval = TimeSpan.FromDays(1);

    /// <summary>
    /// Initializes a new instance of the <see cref="FileVersionStorage"/> class.
    /// </summary>
    /// <param name="stream">The localStream.</param>
    public FileVersionStorage(Stream stream) : base(stream)
    {
    }

    /// <summary>
    /// Compacts the specified storage path.
    /// </summary>
    /// <param name="storagePath">The storage path.</param>
    /// <returns><c>true</c> if the storage path was successfully compacted, <c>false</c> otherwise.</returns>
    /// <remarks>
    /// Keeps the newest entry of each path, and drops the entries of files that were missing when they were read.
    /// At most once per day, it also drops the entries in folders that no longer exist. The write time of
    /// the file <c>&lt;storagePath&gt;.pruned</c> records when this last happened.
    /// </remarks>
    public static bool Compact(string storagePath)
    {
        FileStream fileStreamExclusive;
        try
        {
            fileStreamExclusive = new FileStream(storagePath, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
        }
        catch (Exception)
        {
            return false;
        }

        try
        {
            using var localTracker = new FileVersionStorage(fileStreamExclusive) { UseTransaction = true, AutoLoadNewValues = false };
            localTracker.LoadNewValues();
            var values = localTracker.GetValues();
            var latestVersion = new Dictionary<string, KeyValuePair<FileVersionKey, ObjectId>>(StringComparer.OrdinalIgnoreCase);
            foreach (var keyValue in values)
            {
                // A key without a size describes a missing file (older versions kept their failed reads): it matches no file to hash
                if (keyValue.Key.FileSize < 0)
                    continue;

                var filePath = keyValue.Key.Path;
                if (!latestVersion.TryGetValue(filePath, out var previousKeyValue) || keyValue.Key.LastModifiedDate > previousKeyValue.Key.LastModifiedDate)
                {
                    latestVersion[filePath] = keyValue;
                }
            }

            var pruneMarkerPath = storagePath + ".pruned";
            var prune = DateTime.UtcNow - File.GetLastWriteTimeUtc(pruneMarkerPath) >= PruneInterval;
            if (prune)
                RemoveEntriesInMissingDirectories(latestVersion);

            // Nothing to drop: the file is kept as it is
            if (latestVersion.Count < values.Length)
            {
                localTracker.Reset();
                localTracker.AddValues(latestVersion.Values);
                localTracker.Save();
            }

            if (prune)
                File.WriteAllBytes(pruneMarkerPath, []);
        }
        catch (Exception)
        {
            return false;

        }
        return true;
    }

    private static void RemoveEntriesInMissingDirectories(Dictionary<string, KeyValuePair<FileVersionKey, ObjectId>> latestVersion)
    {
        // One check per folder, and a missing folder answers for its subfolders
        var directoryExists = new Dictionary<string, bool>(StringComparer.Ordinal);
        bool DirectoryExists(string? directory)
        {
            if (string.IsNullOrEmpty(directory))
                return true;

            if (!directoryExists.TryGetValue(directory, out var exists))
            {
                exists = DirectoryExists(Path.GetDirectoryName(directory)) && Directory.Exists(directory);
                directoryExists[directory] = exists;
            }
            return exists;
        }

        foreach (var filePath in latestVersion.Keys.ToList())
        {
            if (!DirectoryExists(Path.GetDirectoryName(filePath)))
                latestVersion.Remove(filePath);
        }
    }

    protected override List<KeyValuePair<FileVersionKey, ObjectId>> ReadEntries(System.IO.Stream localStream)
    {
        // As the FileVersionStorage is not used at runtime but only at build time, it is not currently optimized 
        // TODO: performance of encoding/decoding could be improved using some manual (but much more laborious) code.
        var reader = new StreamReader(localStream, Encoding.UTF8);
        string? line;
        var entries = new List<KeyValuePair<FileVersionKey, ObjectId>>();
        while ((line = reader.ReadLine()) != null)
        {
            var values = line.Split('\t');
            if (values.Length != 4)
            {
                continue;
            }
            // Path: values[0]
            var key = new FileVersionKey() { Path = values[0]};

            if (!long.TryParse(values[1], out var dateTime))
            {
                throw new InvalidOperationException("Unable to decode datetime [{0}] when reading file version index".ToFormat(values[1]));
            }
            key.LastModifiedDate = new DateTime(dateTime);

            if (!long.TryParse(values[2], out key.FileSize))
            {
                throw new InvalidOperationException("Unable to decode filesize [{0}] when reading file version index".ToFormat(values[2]));
            }
            var objectIdStr = values[3];

            if (!ObjectId.TryParse(objectIdStr, out var objectId))
            {
                throw new InvalidOperationException("Unable to decode objectid [{0}] when reading file version index".ToFormat(objectIdStr));
            }

            var entry = new KeyValuePair<FileVersionKey, ObjectId>(key, objectId);
            entries.Add(entry);
        }
        return entries;
    }

    protected override void WriteEntry(Stream localStream, KeyValuePair<FileVersionKey, ObjectId> value)
    {
        ArgumentNullException.ThrowIfNull(localStream);

        var key = value.Key;
        var line = $"{key.Path}\t{key.LastModifiedDate.Ticks}\t{key.FileSize}\t{value.Value}\n";
        var bytes = Encoding.UTF8.GetBytes(line);
        localStream.Write(bytes, 0, bytes.Length);
    }
}
