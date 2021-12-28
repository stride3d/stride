// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Stride.Core;
using Stride.Core.IO;
using Stride.Core.Storage;

namespace Stride.StorageTool
{
    /// <summary>
    /// Description of an object entry in the bundle.
    /// </summary>
    public class ObjectEntry
    {
        public string Location { get; set; }

        public ObjectId Id { get; set; }

        public long Size { get; set; }

        public long SizeNotCompressed { get; set; }

        public override string ToString()
        {
            return string.Format("{0}\t{1}\t{2}\t{3}", Location, Id, Size, SizeNotCompressed);
        }
    }

    /// <summary>
    /// Utility class to manipulate storage and bundles.
    /// </summary>
    public class StorageToolApp
    {
        /// <summary>
        /// List the specified bundle to a txt file and opens it.
        /// </summary>
        /// <param name="bundlePath">The bundle path.</param>
        public static void View(string bundlePath)
        {
            var entries = GetBundleListing(bundlePath);
            var dumpFilePath = bundlePath + ".txt";

            var text = new StringBuilder();
            foreach (var entry in entries)
            {
                text.AppendLine(entry.ToString());
            }
            File.WriteAllText(dumpFilePath, text.ToString());

            System.Diagnostics.Process.Start(dumpFilePath);
        }

        /// <summary>
        /// Gets the listing from a bundle.
        /// </summary>
        /// <param name="bundlePath">The bundle path.</param>
        /// <returns>System.Collections.Generic.List&lt;Stride.StorageTool.ObjectEntry&gt;.</returns>
        private static List<ObjectEntry> GetBundleListing(string bundlePath)
        {
            if (bundlePath == null) throw new ArgumentNullException("bundlePath");
            if (Path.GetExtension(bundlePath) != BundleOdbBackend.BundleExtension) throw new StorageAppException("Invalid bundle file [{0}] not having extension [{1}]".ToFormat(bundlePath, BundleOdbBackend.BundleExtension));
            if (!File.Exists(bundlePath)) throw new StorageAppException("Bundle file [{0}] not found".ToFormat(bundlePath));

            BundleDescription bundle;
            using (var stream = File.OpenRead(bundlePath))
            {
                bundle = BundleOdbBackend.ReadBundleDescription(stream);
            }

            var objectInfos = bundle.Objects.ToDictionary(x => x.Key, x => x.Value);


            var entries = new List<ObjectEntry>();
            foreach (var locationIds in bundle.Assets)
            {
                var entry = new ObjectEntry { Location = locationIds.Key, Id = locationIds.Value };

                BundleOdbBackend.ObjectInfo objectInfo;
                if (objectInfos.TryGetValue(entry.Id, out objectInfo))
                {

                    entry.Size = objectInfo.EndOffset - objectInfo.StartOffset;
                    entry.SizeNotCompressed = objectInfo.SizeNotCompressed;
                }
                entries.Add(entry);
            }

            return entries;
        }
    }
}
