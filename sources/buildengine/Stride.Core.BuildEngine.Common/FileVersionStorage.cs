// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Stride.Core;
using Stride.Core.IO;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Serializers;
using Stride.Core.Storage;

namespace Stride.Core.BuildEngine
{
    /// <summary>
    /// Storage used for <see cref="FileVersionKey"/> associated with an <see cref="ObjectId"/>.
    /// </summary>
    [DataSerializerGlobal(typeof(KeyValuePairSerializer<FileVersionKey, ObjectId>))]
    public sealed class FileVersionStorage : DictionaryStore<FileVersionKey, ObjectId>
    {
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
                using (var localTracker = new FileVersionStorage(fileStreamExclusive) { UseTransaction = true, AutoLoadNewValues = false })
                {
                    localTracker.LoadNewValues();
                    var latestVersion = new Dictionary<string, KeyValuePair<FileVersionKey, ObjectId>>(StringComparer.OrdinalIgnoreCase);
                    foreach (var keyValue in localTracker.GetValues())
                    {
                        var filePath = keyValue.Key.Path;
                        KeyValuePair<FileVersionKey, ObjectId> previousKeyValue;
                        if (!latestVersion.TryGetValue(filePath, out previousKeyValue) || keyValue.Key.LastModifiedDate > previousKeyValue.Key.LastModifiedDate)
                        {
                            latestVersion[filePath] = keyValue;
                        }
                    }
                    localTracker.Reset();
                    localTracker.AddValues(latestVersion.Values);
                    localTracker.Save();
                }
            }
            catch (Exception)
            {
                return false;

            }
            return true;
        }

        protected override List<KeyValuePair<FileVersionKey, ObjectId>> ReadEntries(System.IO.Stream localStream)
        {
            // As the FileVersionStorage is not used at runtime but only at build time, it is not currently optimized 
            // TODO: performance of encoding/decoding could be improved using some manual (but much more laborious) code.
            var reader = new StreamReader(localStream, Encoding.UTF8);
            string line;
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

                long dateTime;
                if (!long.TryParse(values[1], out dateTime))
                {
                    throw new InvalidOperationException("Unable to decode datetime [{0}] when reading file version index".ToFormat(values[1]));
                }
                key.LastModifiedDate = new DateTime(dateTime);

                if (!long.TryParse(values[2], out key.FileSize))
                {
                    throw new InvalidOperationException("Unable to decode filesize [{0}] when reading file version index".ToFormat(values[2]));
                }
                var objectIdStr = values[3];

                ObjectId objectId;
                if (!ObjectId.TryParse(objectIdStr, out objectId))
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
            var key = value.Key;
            var line = string.Format("{0}\t{1}\t{2}\t{3}\n", key.Path, key.LastModifiedDate.Ticks, key.FileSize, value.Value);
            var bytes = Encoding.UTF8.GetBytes(line);
            localStream.Write(bytes, 0, bytes.Length);
        }
    }
}
