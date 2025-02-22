// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Text;
using System.Text.RegularExpressions;
using Stride.Core.IO;
using Stride.Core.Storage;

namespace Stride.Core.Serialization.Contents;

public sealed partial class ContentIndexMap : DictionaryStore<string, ObjectId>, IContentIndexMap
{
    private static readonly Regex regexEntry = RegexEntry();
    private static readonly Regex regexEntrySpace = RegexEntrySpace();

    private ContentIndexMap()
        : base(null)
    {
    }

    public static ContentIndexMap NewTool(string indexName)
    {
        ArgumentNullException.ThrowIfNull(indexName);

        return new ContentIndexMap
        {
            // Try to open with read-write
            stream = VirtualFileSystem.OpenStream(
                VirtualFileSystem.ApplicationDatabasePath + '/' + indexName,
                VirtualFileMode.OpenOrCreate,
                VirtualFileAccess.ReadWrite,
                VirtualFileShare.ReadWrite),
        };
    }

    public static ContentIndexMap CreateInMemory()
    {
        var result = new ContentIndexMap { stream = new MemoryStream() };
        result.LoadNewValues();
        return result;
    }

    public static ContentIndexMap Load(string indexFile, bool isReadOnly = false)
    {
        ArgumentNullException.ThrowIfNull(indexFile);

        var result = new ContentIndexMap();

        var isAppDataWriteable = !isReadOnly;
        if (isAppDataWriteable)
        {
            try
            {
                // Try to open with read-write
                result.stream = VirtualFileSystem.OpenStream(
                    indexFile,
                    VirtualFileMode.OpenOrCreate,
                    VirtualFileAccess.ReadWrite,
                    VirtualFileShare.ReadWrite);
            }
            catch (UnauthorizedAccessException)
            {
                isAppDataWriteable = false;
            }
        }

        if (!isAppDataWriteable)
        {
            // Try to open read-only
            result.stream = VirtualFileSystem.OpenStream(
                indexFile,
                VirtualFileMode.Open,
                VirtualFileAccess.Read);
        }

        result.LoadNewValues();

        return result;
    }

    public IEnumerable<KeyValuePair<string, ObjectId>> GetTransactionIdMap()
    {
        lock (lockObject)
        {
            return GetPendingItems(transaction);
        }
    }

    public IEnumerable<KeyValuePair<string, ObjectId>> GetMergedIdMap()
    {
        lock (lockObject)
        {
            return unsavedIdMap
                .Select(x => new KeyValuePair<string, ObjectId>(x.Key, x.Value.Value))
                .Concat(loadedIdMap.Where(x => !unsavedIdMap.ContainsKey(x.Key)))
                .ToArray();
        }
    }

    protected override List<KeyValuePair<string, ObjectId>> ReadEntries(Stream localStream)
    {
        ArgumentNullException.ThrowIfNull(localStream);

        var reader = new StreamReader(localStream, Encoding.UTF8);
        var entries = new List<KeyValuePair<string, ObjectId>>();
        while (reader.ReadLine() is { } line)
        {
            line = line.Trim();
            if (line.Length == 0 || line.StartsWith('#'))
                continue;

            var match = regexEntry.Match(line);
            if (!match.Success)
            {
                throw new InvalidOperationException("Unable to read asset index entry [{0}]. Expecting: [path objectId]".ToFormat(line));
            }

            var url = match.Groups[1].Value;
            var objectIdStr = match.Groups[2].Value;

            // Test if the name has leading or ending spaces
            var matchSpace = regexEntrySpace.Match(line);
            if (matchSpace.Success && !matchSpace.Groups[1].Value.Equals(url))
            {
                throw new InvalidOperationException("Assets names cannot have empty spaces before or after the name. Please rename [{0}] and compile again.".ToFormat(matchSpace.Groups[1].Value));
            }

            if (!ObjectId.TryParse(objectIdStr, out var objectId))
            {
                throw new InvalidOperationException("Unable to decode objectid [{0}] when reading asset index".ToFormat(objectIdStr));
            }

            var entry = new KeyValuePair<string, ObjectId>(url, objectId);
            entries.Add(entry);
        }
        return entries;
    }

    protected override void WriteEntry(Stream localStream, KeyValuePair<string, ObjectId> value)
    {
        ArgumentNullException.ThrowIfNull(localStream);

        var line = $"{value.Key} {value.Value}\n";
        var bytes = Encoding.UTF8.GetBytes(line);
        localStream.Write(bytes, 0, bytes.Length);
    }

    [GeneratedRegex(@"^(.*?)\s+(\w+)$")]
    private static partial Regex RegexEntry();
    [GeneratedRegex(@"^(.*?)\s(\w+)$")]
    private static partial Regex RegexEntrySpace();
}
