// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Extensions;
using Stride.Core.IO;
using Stride.Core.LZ4;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Contents;
using Stride.Core.Serialization.Serializers;

namespace Stride.Core.Storage
{
    /// <summary>
    /// Object Database Backend (ODB) implementation that bundles multiple chunks into a .bundle files, optionally compressed with LZ4.
    /// </summary>
    [DataSerializerGlobal(null, typeof(List<string>))]
    [DataSerializerGlobal(null, typeof(List<ObjectId>))]
    [DataSerializerGlobal(null, typeof(List<KeyValuePair<ObjectId, BundleOdbBackend.ObjectInfo>>))]
    [DataSerializerGlobal(null, typeof(List<KeyValuePair<string, ObjectId>>))]
    public class BundleOdbBackend : IOdbBackend
    {
        /// <summary>
        /// The bundle file extension.
        /// </summary>
        public const string BundleExtension = ".bundle";

        /// <summary>
        /// The default directory where bundle are stored.
        /// </summary>
        private readonly string vfsBundleDirectory;

        private readonly Dictionary<ObjectId, ObjectLocation> objects = new Dictionary<ObjectId, ObjectLocation>();

        // Bundle name => Bundle VFS URL
        private readonly Dictionary<string, string> resolvedBundles = new Dictionary<string, string>();

        private readonly List<LoadedBundle> loadedBundles = new List<LoadedBundle>(); 

        private readonly ObjectDatabaseContentIndexMap contentIndexMap = new ObjectDatabaseContentIndexMap();

        public delegate Task<string> BundleResolveDelegate(string bundleName);

        /// <summary>
        /// Bundle resolve event asynchronous handler.
        /// </summary>
        public BundleResolveDelegate BundleResolve { get; set; }

        /// <inheritdoc/>
        public IContentIndexMap ContentIndexMap
        {
            get { return contentIndexMap; }
        }

        public string BundleDirectory { get { return vfsBundleDirectory; } }

        public BundleOdbBackend(string vfsRootUrl)
        {
            vfsBundleDirectory = vfsRootUrl + "/bundles/";

            if (!VirtualFileSystem.DirectoryExists(vfsBundleDirectory))
                VirtualFileSystem.CreateDirectory(vfsBundleDirectory);

            BundleResolve += DefaultBundleResolve;
        }

        public void Dispose()
        {
        }

        public Dictionary<ObjectId, ObjectInfo> GetObjectInfos()
        {
            lock (objects)
            {
                return objects.ToDictionary(pair => pair.Key, value => value.Value.Info);
            }
        }

        private Task<string> DefaultBundleResolve(string bundleName)
        {
            // Try to find [bundleName].bundle
            var bundleFile = VirtualFileSystem.Combine(vfsBundleDirectory, bundleName + BundleExtension);
            if (VirtualFileSystem.FileExists(bundleFile))
                return Task.FromResult(bundleFile);

            return Task.FromResult<string>(null);
        }

        private async Task<string> ResolveBundle(string bundleName, bool throwExceptionIfNotFound)
        {
            string bundleUrl;

            lock (resolvedBundles)
            {
                if (resolvedBundles.TryGetValue(bundleName, out bundleUrl))
                {
                    if (bundleUrl == null)
                        throw new InvalidOperationException(string.Format("Bundle {0} is being loaded twice (either cyclic dependency or concurrency issue)", bundleName));
                    return bundleUrl;
                }

                // Store null until resolved (to detect cyclic dependencies)
                resolvedBundles[bundleName] = null;
            }

            if (BundleResolve != null)
            {
                // Iterate over each handler and find the first one that returns non-null result
                foreach (BundleResolveDelegate bundleResolvedHandler in BundleResolve.GetInvocationList())
                {
                    // Use handler to resolve package
                    bundleUrl = await bundleResolvedHandler(bundleName);
                    if (bundleUrl != null)
                        break;
                }
            }

            // Check if it has been properly resolved
            if (bundleUrl == null)
            {
                // Remove from resolved bundles
                lock (resolvedBundles)
                {
                    resolvedBundles.Remove(bundleName);
                }

                if (!throwExceptionIfNotFound)
                    return null;

                throw new FileNotFoundException(string.Format("Bundle {0} could not be resolved", bundleName));
            }

            // Register resolved package
            lock (resolvedBundles)
            {
                resolvedBundles[bundleName] = bundleUrl;
            }

            return bundleUrl;
        }

        /// <summary>
        /// Loads the specified bundle.
        /// </summary>
        /// <param name="bundleName">Name of the bundle.</param>
        /// <param name="objectDatabaseContentIndexMap">The object database asset index map, where newly loaded assets will be merged (ignored if null).</param>
        /// <returns>Task that will complete when bundle is loaded.</returns>
        public async Task LoadBundle(string bundleName, ObjectDatabaseContentIndexMap objectDatabaseContentIndexMap)
        {
            if (bundleName == null) throw new ArgumentNullException("bundleName");

            // Check loaded bundles
            lock (loadedBundles)
            {
                foreach (var currentBundle in loadedBundles)
                {
                    if (currentBundle.BundleName == bundleName)
                    {
                        currentBundle.ReferenceCount++;
                        return;
                    }
                }
            }

            // Resolve package
            var vfsUrl = await ResolveBundle(bundleName, true);

            await LoadBundleFromUrl(bundleName, objectDatabaseContentIndexMap, vfsUrl);
        }

        public async Task LoadBundleFromUrl(string bundleName, ObjectDatabaseContentIndexMap objectDatabaseContentIndexMap, string bundleUrl, bool ignoreDependencies = false)
        {
            List<string> files;
            var bundle = ReadBundleHeader(bundleUrl, out files);

            if (bundle == null)
                throw new FileNotFoundException("Could not find bundle", bundleUrl);

            // Read and resolve dependencies
            if (!ignoreDependencies)
            {
                foreach (var dependency in bundle.Dependencies)
                {
                    await LoadBundle(dependency, objectDatabaseContentIndexMap);
                }
            }

            LoadedBundle loadedBundle = null;

            lock (loadedBundles)
            {
                foreach (var currentBundle in loadedBundles)
                {
                    if (currentBundle.BundleName == bundleName)
                    {
                        loadedBundle = currentBundle;
                        break;
                    }
                }

                if (loadedBundle == null)
                {
                    loadedBundle = new LoadedBundle
                    {
                        BundleName = bundleName,
                        BundleUrl = bundleUrl,
                        Description = bundle,
                        ReferenceCount = 1,
                        Files = files,
                        Streams = new List<Stream>(files.Select(x => (Stream)null)),
                    };

                    loadedBundles.Add(loadedBundle);
                }
                else
                {
                    loadedBundle.ReferenceCount++;
                }
            }

            // Read objects
            lock (objects)
            {
                foreach (var objectEntry in bundle.Objects)
                {
                    objects[objectEntry.Key] = new ObjectLocation { Info = objectEntry.Value, LoadedBundle = loadedBundle };
                }
            }

            // Merge with local (asset bundles) index map
            contentIndexMap.Merge(bundle.Assets);

            // Merge with global object database map
            objectDatabaseContentIndexMap.Merge(bundle.Assets);
        }

        public static BundleDescription ReadBundleHeader(string bundleUrl, out List<string> bundleUrls)
        {
            BundleDescription bundle;

            // If there is a .bundle, add incremental id before it
            var currentBundleExtensionUrl = bundleUrl.Length - (bundleUrl.EndsWith(BundleExtension) ? BundleExtension.Length : 0);

            // Process incremental bundles one by one
            using (var packStream = VirtualFileSystem.OpenStream(bundleUrl, VirtualFileMode.Open, VirtualFileAccess.Read))
            {
                bundle = ReadBundleDescription(packStream);
            }

            bundleUrls = new List<string> { bundleUrl };
            bundleUrls.AddRange(bundle.IncrementalBundles.Select(x => bundleUrl.Insert(currentBundleExtensionUrl, "." + x)));

            return bundle;
        }

        /// <summary>
        /// Unload the specified bundle.
        /// </summary>
        /// <param name="bundleName">Name of the bundle.</param>
        /// <param name="objectDatabaseContentIndexMap">The object database asset index map, where newly loaded assets will be merged (ignored if null).</param>
        public void UnloadBundle(string bundleName, ObjectDatabaseContentIndexMap objectDatabaseContentIndexMap)
        {
            lock (loadedBundles)
            lock (objects)
            {
                // Unload package
                UnloadBundleRecursive(bundleName, objectDatabaseContentIndexMap);

                // Remerge previously loaded packages
                foreach (var otherLoadedBundle in loadedBundles)
                {
                    var bundle = otherLoadedBundle.Description;

                    // Read objects
                    foreach (var objectEntry in bundle.Objects)
                    {
                        objects[objectEntry.Key] = new ObjectLocation { Info = objectEntry.Value, LoadedBundle = otherLoadedBundle };
                    }

                    contentIndexMap.Merge(bundle.Assets);
                    objectDatabaseContentIndexMap.Merge(bundle.Assets);
                }
            }
        }

        private void UnloadBundleRecursive(string bundleName, ObjectDatabaseContentIndexMap objectDatabaseContentIndexMap)
        {
            if (bundleName == null) throw new ArgumentNullException("bundleName");

            lock (loadedBundles)
            {
                int loadedBundleIndex = -1;

                for (int index = 0; index < loadedBundles.Count; index++)
                {
                    var currentBundle = loadedBundles[index];
                    if (currentBundle.BundleName == bundleName)
                    {
                        loadedBundleIndex = index;
                        break;
                    }
                }

                if (loadedBundleIndex == -1)
                    throw new InvalidOperationException("Bundle has not been loaded.");

                var loadedBundle = loadedBundles[loadedBundleIndex];
                var bundle = loadedBundle.Description;
                if (--loadedBundle.ReferenceCount == 0)
                {
                    // Remove and dispose stream from pool
                    lock (loadedBundle.Streams)
                    {
                        for (int index = 0; index < loadedBundle.Streams.Count; index++)
                        {
                            var stream = loadedBundle.Streams[index];
                            stream.Dispose();
                            loadedBundle.Streams[index] = null;
                        }
                    }

                    // Actually unload bundle
                    loadedBundles.RemoveAt(loadedBundleIndex);

                    // Unload objects from index map (if possible, replace with objects of other bundles
                    var removedObjects = new HashSet<ObjectId>();
                    foreach (var objectEntry in bundle.Objects)
                    {
                        objects.Remove(objectEntry.Key);
                        removedObjects.Add(objectEntry.Key);
                    }

                    // Unmerge with local (asset bundles) index map
                    contentIndexMap.Unmerge(bundle.Assets);

                    // Unmerge with global object database map
                    objectDatabaseContentIndexMap.Unmerge(bundle.Assets);

                    // Remove dependencies too
                    foreach (var dependency in bundle.Dependencies)
                    {
                        UnloadBundleRecursive(dependency, objectDatabaseContentIndexMap);
                    }
                }
            }
        }

        private static bool ValidateHeader(Stream stream)
        {
            var binaryReader = new BinarySerializationReader(stream);

            // Read header
            var header = binaryReader.Read<Header>();

            var result = new BundleDescription();
            result.Header = header;

            // Check magic header
            if (header.MagicHeader != Header.MagicHeaderValid)
            {
                return false;
            }

            // Ensure size has properly been set
            if (header.Size != stream.Length)
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Reads the bundle description.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns>The bundle description.</returns>
        /// <exception cref="System.InvalidOperationException">
        /// Invalid bundle header
        /// or
        /// Bundle has not been properly written
        /// </exception>
        public static BundleDescription ReadBundleDescription(Stream stream)
        {
            var binaryReader = new BinarySerializationReader(stream);

            // Read header
            var header = binaryReader.Read<Header>();

            var result = new BundleDescription();
            result.Header = header;

            // Check magic header
            if (header.MagicHeader != Header.MagicHeaderValid)
            {
                throw new InvalidOperationException("Invalid bundle header");
            }

            // Ensure size has properly been set
            if (header.Size != stream.Length)
            {
                throw new InvalidOperationException("Bundle has not been properly written");
            }

            // Read dependencies
            var dependencies = result.Dependencies;
            binaryReader.Serialize(ref dependencies, ArchiveMode.Deserialize);

            // Read incremental bundles
            var incrementalBundles = result.IncrementalBundles;
            binaryReader.Serialize(ref incrementalBundles, ArchiveMode.Deserialize);

            // Read objects
            var objects = result.Objects;
            binaryReader.Serialize(ref objects, ArchiveMode.Deserialize);

            // Read assets
            var assets = result.Assets;
            binaryReader.Serialize(ref assets, ArchiveMode.Deserialize);

            return result;
        }

        public static void CreateBundle(string bundleUrl, IOdbBackend backend, ObjectId[] objectIds, ISet<ObjectId> disableCompressionIds, Dictionary<string, ObjectId> indexMap, IList<string> dependencies, bool useIncrementalBundle)
        {
            if (objectIds.Length == 0)
                throw new InvalidOperationException("Nothing to pack.");

            var objectsToIndex = new Dictionary<ObjectId, int>(objectIds.Length);

            var objects = new List<KeyValuePair<ObjectId, ObjectInfo>>();
            for (int i = 0; i < objectIds.Length; ++i)
            {
                objectsToIndex.Add(objectIds[i], objects.Count);
                objects.Add(new KeyValuePair<ObjectId, ObjectInfo>(objectIds[i], new ObjectInfo()));
            }

            var incrementalBundles = new List<ObjectId>();

            // If there is a .bundle, add incremental id before it
            var bundleExtensionLength = (bundleUrl.EndsWith(BundleExtension) ? BundleExtension.Length : 0);

            // Early exit if package didn't change (header-check only)
            if (VirtualFileSystem.FileExists(bundleUrl))
            {
                try
                {
                    using (var packStream = VirtualFileSystem.OpenStream(bundleUrl, VirtualFileMode.Open, VirtualFileAccess.Read))
                    {
                        var bundle = ReadBundleDescription(packStream);

                        // If package didn't change since last time, early exit!
                        if (ArrayExtensions.ArraysEqual(bundle.Dependencies, dependencies)
                            && ArrayExtensions.ArraysEqual(bundle.Assets.OrderBy(x => x.Key).ToList(), indexMap.OrderBy(x => x.Key).ToList())
                            && ArrayExtensions.ArraysEqual(bundle.Objects.Select(x => x.Key).OrderBy(x => x).ToList(), objectIds.OrderBy(x => x).ToList()))
                        {
                            // Make sure all incremental bundles exist
                            // Also, if we don't want incremental bundles but we have some (or vice-versa), let's force a regeneration
                            if ((useIncrementalBundle == (bundle.IncrementalBundles.Count > 0))
                                && bundle.IncrementalBundles.Select(x => bundleUrl.Insert(bundleUrl.Length - bundleExtensionLength, "." + x)).All(x =>
                            {
                                if (!VirtualFileSystem.FileExists(x))
                                    return false;
                                using (var incrementalStream = VirtualFileSystem.OpenStream(x, VirtualFileMode.Open, VirtualFileAccess.Read))
                                    return ValidateHeader(incrementalStream);
                            }))
                            {
                                return;
                            }
                        }
                    }

                    // Process existing incremental bundles one by one
                    // Try to find if there is enough to reuse in each of them
                    var filename = VirtualFileSystem.GetFileName(bundleUrl);
                    var directory = VirtualFileSystem.GetParentFolder(bundleUrl);

                    foreach (var incrementalBundleUrl in VirtualFileSystem.ListFiles(directory, filename.Insert(filename.Length - bundleExtensionLength, ".*"), VirtualSearchOption.TopDirectoryOnly).Result)
                    {
                        var incrementalIdString = incrementalBundleUrl.Substring(incrementalBundleUrl.Length - bundleExtensionLength - ObjectId.HashStringLength, ObjectId.HashStringLength);
                        ObjectId incrementalId;
                        if (!ObjectId.TryParse(incrementalIdString, out incrementalId))
                            continue;

                        // If we don't want incremental bundles, delete old ones from previous build
                        if (!useIncrementalBundle)
                        {
                            VirtualFileSystem.FileDelete(incrementalBundleUrl);
                            continue;
                        }

                        long sizeNeededItems = 0;
                        long sizeTotal = 0;

                        BundleDescription incrementalBundle;
                        try
                        {
                            using (var packStream = VirtualFileSystem.OpenStream(incrementalBundleUrl, VirtualFileMode.Open, VirtualFileAccess.Read))
                            {
                                incrementalBundle = ReadBundleDescription(packStream);
                            }

                            // Compute size of objects (needed ones and everything)
                            foreach (var @object in incrementalBundle.Objects)
                            {
                                var objectCompressedSize = @object.Value.EndOffset - @object.Value.StartOffset;

                                // TODO: Detect object that are stored without ObjectId being content hash: we need to check actual content hash is same in this case
                                if (objectsToIndex.ContainsKey(@object.Key))
                                    sizeNeededItems += objectCompressedSize;
                                sizeTotal += objectCompressedSize;
                            }

                            // Check if we would reuse at least 50% of the incremental bundle, otherwise let's just get rid of it
                            var reuseRatio = (float)((double)sizeNeededItems / (double)sizeTotal);
                            if (reuseRatio < 0.5f)
                            {
                                VirtualFileSystem.FileDelete(incrementalBundleUrl);
                            }
                            else
                            {
                                // We will reuse this incremental bundle
                                // Let's add ObjectId entries
                                foreach (var @object in incrementalBundle.Objects)
                                {
                                    int objectIndex;
                                    if (objectsToIndex.TryGetValue(@object.Key, out objectIndex))
                                    {
                                        var objectInfo = @object.Value;
                                        objectInfo.IncrementalBundleIndex = incrementalBundles.Count + 1;
                                        objects[objectIndex] = new KeyValuePair<ObjectId, ObjectInfo>(@object.Key, objectInfo);
                                    }
                                }

                                // Add this incremental bundle in the list
                                incrementalBundles.Add(incrementalId);
                            }
                        }
                        catch (Exception)
                        {
                            // Could not read incremental bundle (format changed?)
                            // Let's delete it
                            VirtualFileSystem.FileDelete(incrementalBundleUrl);
                        }
                    }
                }
                catch (Exception)
                {
                    // Could not read previous bundle (format changed?)
                    // Let's just mute this error as new bundle will overwrite it anyway
                }
            }

            // Count objects which needs to be saved
            var incrementalObjects = new List<KeyValuePair<ObjectId, ObjectInfo>>();
            if (useIncrementalBundle)
            {
                for (int i = 0; i < objectIds.Length; ++i)
                {
                    // Skip if already part of an existing incremental package
                    if (objects[i].Value.IncrementalBundleIndex > 0)
                        continue;

                    incrementalObjects.Add(new KeyValuePair<ObjectId, ObjectInfo>(objects[i].Key, new ObjectInfo()));
                }
            }

            // Create an incremental package
            var newIncrementalId = ObjectId.New();
            var incrementalBundleIndex = incrementalBundles.Count;
            if (useIncrementalBundle && incrementalObjects.Count > 0)
                incrementalBundles.Add(newIncrementalId);

            using (var packStream = VirtualFileSystem.OpenStream(bundleUrl, VirtualFileMode.Create, VirtualFileAccess.Write))
            {
                var header = new Header();
                header.MagicHeader = Header.MagicHeaderValid;

                var packBinaryWriter = new BinarySerializationWriter(packStream);
                packBinaryWriter.Write(header);
                // Write dependencies
                packBinaryWriter.Write(dependencies.ToList());
                // Write inecremental bundles
                packBinaryWriter.Write(incrementalBundles.ToList());

                // Save location of object ids
                var packObjectIdPosition = packStream.Position;

                // Write empty object ids (reserve space, will be rewritten later)
                packBinaryWriter.Write(objects);

                // Write index
                packBinaryWriter.Write(indexMap.ToList());

                using (var incrementalStream = incrementalObjects.Count > 0 ? VirtualFileSystem.OpenStream(bundleUrl.Insert(bundleUrl.Length - bundleExtensionLength, "." + newIncrementalId), VirtualFileMode.Create, VirtualFileAccess.Write) : null)
                {
                    var incrementalBinaryWriter = incrementalStream != null ? new BinarySerializationWriter(incrementalStream) : null;
                    long incrementalObjectIdPosition = 0;
                    if (incrementalStream != null)
                    {
                        incrementalBinaryWriter.Write(header);
                        // Write dependencies
                        incrementalBinaryWriter.Write(new List<string>());
                        // Write inecremental bundles
                        incrementalBinaryWriter.Write(new List<ObjectId>());

                        // Save location of object ids
                        incrementalObjectIdPosition = incrementalStream.Position;

                        // Write empty object ids (reserve space, will be rewritten later)
                        incrementalBinaryWriter.Write(incrementalObjects);

                        // Write index
                        incrementalBinaryWriter.Write(new List<KeyValuePair<string, ObjectId>>());
                    }

                    var objectOutputStream = incrementalStream ?? packStream;
                    int incrementalObjectIndex = 0;
                    for (int i = 0; i < objectIds.Length; ++i)
                    {
                        // Skip if already part of an existing incremental package
                        if (objects[i].Value.IncrementalBundleIndex > 0)
                            continue;

                        using (var objectStream = backend.OpenStream(objectIds[i]))
                        {
                            // Prepare object info
                            var objectInfo = new ObjectInfo { StartOffset = objectOutputStream.Position, SizeNotCompressed = objectStream.Length };

                            // re-order the file content so that it is not necessary to seek while reading the input stream (header/object/refs -> header/refs/object)
                            var inputStream = objectStream;
                            var originalStreamLength = objectStream.Length;
                            var streamReader = new BinarySerializationReader(inputStream);
                            var chunkHeader = ChunkHeader.Read(streamReader);
                            if (chunkHeader != null)
                            {
                                // create the reordered stream
                                var reorderedStream = new MemoryStream((int)originalStreamLength);

                                // copy the header
                                var streamWriter = new BinarySerializationWriter(reorderedStream);
                                chunkHeader.Write(streamWriter);

                                // copy the references
                                var newOffsetReferences = reorderedStream.Position;
                                inputStream.Position = chunkHeader.OffsetToReferences;
                                inputStream.CopyTo(reorderedStream);

                                // copy the object
                                var newOffsetObject = reorderedStream.Position;
                                inputStream.Position = chunkHeader.OffsetToObject;
                                inputStream.CopyTo(reorderedStream, chunkHeader.OffsetToReferences - chunkHeader.OffsetToObject);

                                // rewrite the chunk header with correct offsets
                                chunkHeader.OffsetToObject = (int)newOffsetObject;
                                chunkHeader.OffsetToReferences = (int)newOffsetReferences;
                                reorderedStream.Position = 0;
                                chunkHeader.Write(streamWriter);

                                // change the input stream to use reordered stream
                                inputStream = reorderedStream;
                                inputStream.Position = 0;
                            }

                            // compress the stream
                            if (!disableCompressionIds.Contains(objectIds[i]))
                            {
                                objectInfo.IsCompressed = true;

                                var lz4OutputStream = new LZ4Stream(objectOutputStream, CompressionMode.Compress);
                                inputStream.CopyTo(lz4OutputStream);
                                lz4OutputStream.Flush();
                            }
                            // copy the stream "as is"
                            else
                            {
                                // Write stream
                                inputStream.CopyTo(objectOutputStream);
                            }

                            // release the reordered created stream
                            if (chunkHeader != null)
                                inputStream.Dispose();

                            // Add updated object info
                            objectInfo.EndOffset = objectOutputStream.Position;
                            // Note: we add 1 because 0 is reserved for self; first incremental bundle starts at 1
                            objectInfo.IncrementalBundleIndex = objectOutputStream == incrementalStream ? incrementalBundleIndex + 1 : 0;
                            objects[i] = new KeyValuePair<ObjectId, ObjectInfo>(objectIds[i], objectInfo);

                            if (useIncrementalBundle)
                            {
                                // Also update incremental bundle object info
                                objectInfo.IncrementalBundleIndex = 0; // stored in same bundle
                                incrementalObjects[incrementalObjectIndex++] = new KeyValuePair<ObjectId, ObjectInfo>(objectIds[i], objectInfo);
                            }
                        }
                    }

                    // First finish to write incremental package so that main one can't be valid on the HDD without the incremental one being too
                    if (incrementalStream != null)
                    {
                        // Rewrite headers
                        header.Size = incrementalStream.Length;
                        incrementalStream.Position = 0;
                        incrementalBinaryWriter.Write(header);

                        // Rewrite object with updated offsets/size
                        incrementalStream.Position = incrementalObjectIdPosition;
                        incrementalBinaryWriter.Write(incrementalObjects);
                    }
                }

                // Rewrite headers
                header.Size = packStream.Length;
                packStream.Position = 0;
                packBinaryWriter.Write(header);

                // Rewrite object with updated offsets/size
                packStream.Position = packObjectIdPosition;
                packBinaryWriter.Write(objects);
            }
        }

        public Stream OpenStream(ObjectId objectId, VirtualFileMode mode = VirtualFileMode.Open, VirtualFileAccess access = VirtualFileAccess.Read, VirtualFileShare share = VirtualFileShare.Read)
        {
            ObjectLocation objectLocation;
            lock (objects)
            {
                if (!objects.TryGetValue(objectId, out objectLocation))
                    throw new FileNotFoundException();
            }

            var loadedBundle = objectLocation.LoadedBundle;
            var streams = objectLocation.LoadedBundle.Streams;
            Stream stream;

            // Try to reuse same streams
            lock (streams)
            {
                // Available stream?
                if ((stream = streams[objectLocation.Info.IncrementalBundleIndex]) != null)
                {
                    // Remove from available streams
                    streams[objectLocation.Info.IncrementalBundleIndex] = null;
                }
                else
                {
                    stream = VirtualFileSystem.OpenStream(loadedBundle.Files[objectLocation.Info.IncrementalBundleIndex], VirtualFileMode.Open, VirtualFileAccess.Read);
                }
            }

            if (objectLocation.Info.IsCompressed)
            {
                stream.Position = objectLocation.Info.StartOffset;
                return new PackageFileStreamLZ4(this, objectLocation, stream, CompressionMode.Decompress, objectLocation.Info.SizeNotCompressed, objectLocation.Info.EndOffset - objectLocation.Info.StartOffset);
            }

            return new PackageFileStream(this, objectLocation, stream, objectLocation.Info.StartOffset, objectLocation.Info.EndOffset, false);
        }

        public int GetSize(ObjectId objectId)
        {
            lock (objects)
            {
                var objectInfo = objects[objectId].Info;
                return (int)(objectInfo.EndOffset - objectInfo.StartOffset);
            }
        }

        public ObjectId Write(ObjectId objectId, Stream dataStream, int length, bool forceWrite)
        {
            throw new NotSupportedException();
        }

        public OdbStreamWriter CreateStream()
        {
            throw new NotSupportedException();
        }

        public bool Exists(ObjectId objectId)
        {
            lock (objects)
            {
                return objects.ContainsKey(objectId);
            }
        }

        public IEnumerable<ObjectId> EnumerateObjects()
        {
            lock (objects)
            {
                return objects.Select(x => x.Key).ToList();
            }
        }

        public void Delete(ObjectId objectId)
        {
            throw new NotSupportedException();
        }

        public string GetFilePath(ObjectId objectId)
        {
            throw new NotSupportedException();
        }

        public bool TryGetObjectLocation(ObjectId objectId, out string filePath, out long start, out long end)
        {
            start = 0;
            end = -1;
            filePath = null;
            lock (objects)
            {
                // Ask location of this object in the bundle
                if (!objects.TryGetValue(objectId, out ObjectLocation location))
                    return false;

                var info = location.Info;
                var path = location.LoadedBundle.Files[info.IncrementalBundleIndex];
                var providerResult = VirtualFileSystem.ResolveProvider(path, false);

                // Get info about where the bundle is stored (could be its own file or stored in another file)
                var result = providerResult.Provider.TryGetFileLocation(providerResult.Path, out filePath, out var bundleStart, out var bundleEnd);
                if (!result)
                    return false;

                start = bundleStart + info.StartOffset;
                end = bundleStart + info.EndOffset;
                return true;
            }
        }

        private struct ObjectLocation
        {
            public ObjectInfo Info;
            public LoadedBundle LoadedBundle;
        }

        private class LoadedBundle
        {
            public string BundleName;
            public string BundleUrl;
            public int ReferenceCount;
            public BundleDescription Description;

            // Stream pool to avoid reopening same file multiple time (list, one per incremental file)
            public List<string> Files;
            public List<Stream> Streams;
        }

        private void ReleasePackageStream(ObjectLocation objectLocation, Stream stream)
        {
            var loadedBundle = objectLocation.LoadedBundle;
            lock (loadedBundle.Streams)
            {
                if (loadedBundle.Streams[objectLocation.Info.IncrementalBundleIndex] == null)
                {
                    loadedBundle.Streams[objectLocation.Info.IncrementalBundleIndex] = stream;
                }
                else
                {
                    stream.Dispose();
                }
            }
        }

        [DataContract]
        [DataSerializer(typeof(Serializer))]
        public struct ObjectInfo
        {
            public long StartOffset;
            public long EndOffset;
            public long SizeNotCompressed;
            public bool IsCompressed;

            // Note: 0 means self, remove 1 to get index in BundleDescription.IncrementalBundles
            public int IncrementalBundleIndex;

            internal class Serializer : DataSerializer<ObjectInfo>
            {
                public override void Serialize(ref ObjectInfo obj, ArchiveMode mode, SerializationStream stream)
                {
                    stream.Serialize(ref obj.StartOffset);
                    stream.Serialize(ref obj.EndOffset);
                    stream.Serialize(ref obj.SizeNotCompressed);
                    stream.Serialize(ref obj.IsCompressed);
                    stream.Serialize(ref obj.IncrementalBundleIndex);
                }
            }
        }

        [DataContract]
        [DataSerializer(typeof(Header.Serializer))]
        public struct Header
        {
            public const uint MagicHeaderValid = 0x31424B58; // "XKB1"

            public uint MagicHeader;
            public long Size;
            public uint Crc; // currently unused

            internal class Serializer : DataSerializer<Header>
            {
                public override void Serialize(ref Header obj, ArchiveMode mode, SerializationStream stream)
                {
                    stream.Serialize(ref obj.MagicHeader);
                    stream.Serialize(ref obj.Size);
                    stream.Serialize(ref obj.Crc);
                }
            }
        }
        private class PackageFileStreamLZ4 : LZ4Stream
        {
            private readonly BundleOdbBackend bundleOdbBackend;
            private readonly ObjectLocation objectLocation;
            private readonly Stream innerStream;

            public PackageFileStreamLZ4(BundleOdbBackend bundleOdbBackend, ObjectLocation objectLocation, Stream innerStream, CompressionMode compressionMode, long uncompressedStreamSize, long compressedSize)
                : base(innerStream, compressionMode, uncompressedSize: uncompressedStreamSize, compressedSize: compressedSize, disposeInnerStream: false)
            {
                this.bundleOdbBackend = bundleOdbBackend;
                this.objectLocation = objectLocation;
                this.innerStream = innerStream;
            }

            protected override void Dispose(bool disposing)
            {
                bundleOdbBackend.ReleasePackageStream(objectLocation, innerStream);

                base.Dispose(disposing);
            }
        }

        private class PackageFileStream : VirtualFileStream
        {
            private readonly BundleOdbBackend bundleOdbBackend;
            private readonly ObjectLocation objectLocation;

            public PackageFileStream(BundleOdbBackend bundleOdbBackend, ObjectLocation objectLocation, Stream internalStream, long startPosition = 0, long endPosition = -1, bool disposeInternalStream = true, bool seekToBeginning = true)
                : base(internalStream, startPosition, endPosition, disposeInternalStream, seekToBeginning)
            {
                this.bundleOdbBackend = bundleOdbBackend;
                this.objectLocation = objectLocation;
            }

            protected override void Dispose(bool disposing)
            {
                bundleOdbBackend.ReleasePackageStream(objectLocation, virtualFileStream ?? InternalStream);

                // If there was a VirtualFileStream, we don't want it to be released as it has been pushed back in the stream pool
                virtualFileStream = null;

                base.Dispose(disposing);
            }
        }

        public void DeleteBundles(Func<string, bool> bundleFileDeletePredicate)
        {
            var bundleFiles = VirtualFileSystem.ListFiles(vfsBundleDirectory, "*" + BundleExtension, VirtualSearchOption.TopDirectoryOnly).Result;

            // Group incremental bundles together
            var bundleFilesGroups = bundleFiles.GroupBy(bundleUrl =>
            {
                // Remove incremental ID from bundle url
                ObjectId incrementalId;
                var filename = VirtualFileSystem.GetFileName(bundleUrl);
                var bundleExtensionLength = filename.EndsWith(BundleExtension) ? BundleExtension.Length : 0;
                if (filename.Length - bundleExtensionLength >= ObjectId.HashStringLength + 1 && filename[filename.Length - bundleExtensionLength - ObjectId.HashStringLength - 1] == '.'
                    && ObjectId.TryParse(filename.Substring(filename.Length - bundleExtensionLength - ObjectId.HashStringLength, ObjectId.HashStringLength), out incrementalId))
                {
                    bundleUrl = bundleUrl.Remove(bundleUrl.Length - bundleExtensionLength - ObjectId.HashStringLength - 1, 1 + ObjectId.HashStringLength);
                }

                return bundleUrl;
            });

            foreach (var bundleFilesInGroup in bundleFilesGroups)
            {
                var bundleMainFile = VirtualFileSystem.GetAbsolutePath(bundleFilesInGroup.Key);

                if (bundleFileDeletePredicate(bundleMainFile))
                {
                    foreach (var bundleRealFile in bundleFilesInGroup)
                        File.Delete(VirtualFileSystem.GetAbsolutePath(bundleRealFile));
                }
            }
        }
    }
}
