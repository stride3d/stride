// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Stride.Core.IO;
using Stride.Core.Storage;

namespace Stride.Core.Serialization.Contents
{
    public sealed class ContentIndexMap : DictionaryStore<string, ObjectId>, IContentIndexMap
    {
        private static readonly Regex RegexEntry = new Regex(@"^(.*?)\s+(\w+)$");
        private static readonly Regex RegexEntrySpace = new Regex(@"^(.*?)\s(\w+)$");

        private ContentIndexMap()
            : base(null)
        {
        }

        public static ContentIndexMap NewTool(string indexName)
        {
            if (indexName == null) throw new ArgumentNullException(nameof(indexName));

            var result = new ContentIndexMap
            {
                // Try to open with read-write
                stream = VirtualFileSystem.OpenStream(
                    VirtualFileSystem.ApplicationDatabasePath + '/' + indexName,
                    VirtualFileMode.OpenOrCreate,
                    VirtualFileAccess.ReadWrite,
                    VirtualFileShare.ReadWrite),
            };

            return result;
        }

        public static ContentIndexMap CreateInMemory()
        {
            var result = new ContentIndexMap { stream = new MemoryStream() };
            result.LoadNewValues();
            return result;
        }

        public static ContentIndexMap Load(string indexFile, bool isReadOnly = false)
        {
            if (indexFile == null) throw new ArgumentNullException(nameof(indexFile));

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
            var reader = new StreamReader(localStream, Encoding.UTF8);
            string line;
            var entries = new List<KeyValuePair<string, ObjectId>>();
            while ((line = reader.ReadLine()) != null)
            {
                line = line.Trim();
                if (line == string.Empty || line.StartsWith("#"))
                    continue;

                var match = RegexEntry.Match(line);
                if (!match.Success)
                {
                    throw new InvalidOperationException("Unable to read asset index entry [{0}]. Expecting: [path objectId]".ToFormat(line));
                }

                var url = match.Groups[1].Value;
                var objectIdStr = match.Groups[2].Value;

                // Test if the name has leading or ending spaces
                var matchSpace = RegexEntrySpace.Match(line);
                if (matchSpace.Success)
                {
                    if (!matchSpace.Groups[1].Value.Equals(url))
                    {
                        throw new InvalidOperationException("Assets names cannot have empty spaces before or after the name. Please rename [{0}] and compile again.".ToFormat(matchSpace.Groups[1].Value));
                    }
                }

                ObjectId objectId;
                if (!ObjectId.TryParse(objectIdStr, out objectId))
                {
                    throw new InvalidOperationException("Unable to decode objectid [{0}] when reading asset index".ToFormat(objectIdStr));
                }

                var entry = new KeyValuePair<string, ObjectId>(url, objectId);
                entries.Add(entry);
            }
            return entries;
        }

        protected override void WriteEntry(Stream stream, KeyValuePair<string, ObjectId> value)
        {
            var line = $"{value.Key} {value.Value}\n";
            var bytes = Encoding.UTF8.GetBytes(line);
            stream.Write(bytes, 0, bytes.Length);
        }
    }
}
