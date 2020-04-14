// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Threading;
using Xenko.Core.Assets.Tracking;
using Xenko.Core.Assets.Yaml;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.IO;

namespace Xenko.Core.Assets
{
    /// <summary>
    /// An asset item part of a <see cref="Package"/> accessible through <see cref="Assets.Package.Assets"/>.
    /// </summary>
    [DataContract("AssetItem")]
    public sealed class AssetItem : IFileSynchronizable
    {
        private UFile location;
        private Asset asset;
        private bool isDirty;
        private HashSet<UFile> sourceFiles;

        /// <summary>
        /// The default comparer use only the id of an assetitem to match assets.
        /// </summary>
        public static readonly IEqualityComparer<AssetItem> DefaultComparerById = new AssetItemComparerById();

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetItem" /> class.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="asset">The asset.</param>
        /// <exception cref="ArgumentNullException">location</exception>
        /// <exception cref="ArgumentNullException">asset</exception>
        public AssetItem([NotNull] UFile location, [NotNull] Asset asset) : this(location, asset, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetItem" /> class.
        /// </summary>
        /// <param name="location">The location.</param>
        /// <param name="asset">The asset.</param>
        /// <param name="package">The package.</param>
        /// <exception cref="ArgumentNullException">location</exception>
        /// <exception cref="ArgumentNullException">asset</exception>
        internal AssetItem([NotNull] UFile location, [NotNull] Asset asset, Package package)
        {
            this.location = location ?? throw new ArgumentNullException(nameof(location));
            this.asset = asset ?? throw new ArgumentNullException(nameof(asset));
            Package = package;
            isDirty = true;
        }

        /// <summary>
        /// Gets the location of this asset.
        /// </summary>
        /// <value>The location.</value>
        [NotNull]
        public UFile Location { get => location; internal set => location = value ?? throw new ArgumentNullException(nameof(value)); }

        /// <summary>
        /// Gets the directory where the assets will be stored on the disk relative to the <see cref="Package"/>. The directory
        /// will update the list found in <see cref="Package.AssetFolders"/>
        /// </summary>
        /// <value>The directory.</value>
        public UDirectory SourceFolder { get; set; }

        /// <summary>
        /// Gets the unique identifier of this asset.
        /// </summary>
        /// <value>The unique identifier.</value>
        public AssetId Id => asset.Id;

        /// <summary>
        /// Gets the package where this asset is stored.
        /// </summary>
        /// <value>The package.</value>
        public Package Package { get; internal set; }

        /// <summary>
        /// Gets the attached metadata for YAML serialization.
        /// </summary>
        [DataMemberIgnore]
        public AttachedYamlAssetMetadata YamlMetadata { get; } = new AttachedYamlAssetMetadata();

        /// <summary>
        /// Converts this item to a reference.
        /// </summary>
        /// <returns>AssetReference.</returns>
        [NotNull]
        public AssetReference ToReference()
        {
            return new AssetReference(Id, Location);
        }

        /// <summary>
        /// Clones this instance without the attached package.
        /// </summary>
        /// <param name="newLocation">The new location that will be used in the cloned <see cref="AssetItem"/>. If this parameter
        /// is null, it keeps the original location of the asset.</param>
        /// <param name="newAsset">The new asset that will be used in the cloned <see cref="AssetItem"/>. If this parameter
        /// is null, it clones the original asset. otherwise, the specified asset is used as-is in the new <see cref="AssetItem"/>
        /// (no clone on newAsset is performed)</param>
        /// <param name="flags">Flags used with <see cref="AssetCloner.Clone"/>.</param>
        /// <returns>A clone of this instance.</returns>
        [NotNull]
        public AssetItem Clone(UFile newLocation = null, Asset newAsset = null, AssetClonerFlags flags = AssetClonerFlags.None)
        {
            return Clone(false, newLocation, newAsset, flags);
        }

        /// <summary>
        /// Clones this instance without the attached package.
        /// </summary>
        /// <param name="keepPackage">if set to <c>true</c> copy package information, only used by the <see cref="Analysis.AssetDependencyManager" />.</param>
        /// <param name="newLocation">The new location that will be used in the cloned <see cref="AssetItem" />. If this parameter
        /// is null, it keeps the original location of the asset.</param>
        /// <param name="newAsset">The new asset that will be used in the cloned <see cref="AssetItem" />. If this parameter
        /// is null, it clones the original asset. otherwise, the specified asset is used as-is in the new <see cref="AssetItem" />
        /// (no clone on newAsset is performed)</param>
        /// <param name="flags">Flags used with <see cref="AssetCloner.Clone"/>.</param>
        /// <returns>A clone of this instance.</returns>
        [NotNull]
        public AssetItem Clone(bool keepPackage, UFile newLocation = null, Asset newAsset = null, AssetClonerFlags flags = AssetClonerFlags.None)
        {
            // Set the package after the new AssetItem(), to make sure that isDirty is not going to call a notification on the
            // package
            var item = new AssetItem(newLocation ?? location, newAsset ?? AssetCloner.Clone(Asset, flags), keepPackage ? Package : null)
            {
                isDirty = isDirty,
                SourceFolder = SourceFolder,
                version = Version,
            };
            YamlMetadata.CopyInto(item.YamlMetadata);
            return item;
        }

        /// <summary>
        /// Gets the full absolute path of this asset on the disk, taking into account the <see cref="SourceFolder"/>, and the
        /// <see cref="Assets.Package.RootDirectory"/>. See remarks.
        /// </summary>
        /// <value>The full absolute path of this asset on the disk.</value>
        /// <remarks>
        /// This value is only valid if this instance is attached to a <see cref="Package"/>, and that the package has
        /// a non null <see cref="Assets.Package.RootDirectory"/>.
        /// </remarks>
        [NotNull]
        public UFile FullPath
        {
            get
            {
                var localSourceFolder = SourceFolder ?? (Package != null ?
                    Package.GetDefaultAssetFolder()
                    : UDirectory.This );

                // Root directory of package
                var rootDirectory = Package != null && Package.RootDirectory != null ? Package.RootDirectory : null;

                // If the source folder is absolute, make it relative to the root directory
                if (localSourceFolder.IsAbsolute)
                {
                    if (rootDirectory != null)
                    {
                        localSourceFolder = localSourceFolder.MakeRelative(rootDirectory);
                    }
                }

                rootDirectory = rootDirectory != null ? UPath.Combine(rootDirectory, localSourceFolder) : localSourceFolder;

                var locationAndExtension = new UFile(Location + AssetRegistry.GetDefaultExtension(Asset.GetType()));
                return rootDirectory != null ? UPath.Combine(rootDirectory, locationAndExtension) : locationAndExtension;
            }
        }

        /// <summary>
        /// Gets or sets the asset.
        /// </summary>
        /// <value>The asset.</value>
        [NotNull]
        public Asset Asset { get => asset; internal set => asset = value ?? throw new ArgumentNullException(nameof(value)); }

        /// <summary>
        /// Gets the modified time. See remarks.
        /// </summary>
        /// <value>The modified time.</value>
        /// <remarks>
        /// By default, contains the last modified time of the asset from the disk. If IsDirty is also updated from false to true
        /// , this time will get current time of modification.
        /// </remarks>
        public DateTime ModifiedTime { get; internal set; }

        private long version;

        /// <summary>
        /// Gets the asset version incremental counter, increased everytime the asset is edited.
        /// </summary>
        public long Version { get => Interlocked.Read(ref version); internal set => Interlocked.Exchange(ref version, value); }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is dirty. See remarks.
        /// </summary>
        /// <value><c>true</c> if this instance is dirty; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// When an asset is modified, this property must be set to true in order to track assets changes.
        /// </remarks>
        public bool IsDirty
        {
            get => isDirty;
            set
            {
                if (value && !isDirty)
                {
                    ModifiedTime = DateTime.Now;
                }

                Interlocked.Increment(ref version);
                sourceFiles?.Clear();

                var oldValue = isDirty;
                isDirty = value;
                Package?.OnAssetDirtyChanged(this, oldValue, value);
            }
        }

        public bool IsDeleted { get; set; }

        public override string ToString()
        {
            return $"[{Asset.GetType().Name}] {location}";
        }

        /// <summary>
        /// Creates a child asset that is inheriting the values of this asset.
        /// </summary>
        /// <returns>A new asset inheriting the values of this asset.</returns>
        [NotNull]
        public Asset CreateDerivedAsset()
        {
            Dictionary<Guid, Guid> idRemapping;
            return Asset.CreateDerivedAsset(Location, out idRemapping);
        }

        /// <summary>
        /// Finds the base item referenced by this item from the current session (using the <see cref="Package"/> setup
        /// on this instance)
        /// </summary>
        /// <returns>The base item or null if not found.</returns>
        [CanBeNull]
        public AssetItem FindBase()
        {
            if (Package?.Session == null || Asset.Archetype == null)
            {
                return null;
            }
            var session = Package.Session;
            return session.FindAsset(Asset.Archetype.Id);
        }

        /// <summary>
        /// In case <see cref="SourceFolder"/> was null, generates it.
        /// </summary>
        public void UpdateSourceFolders()
        {
            Package.UpdateSourceFolders(new[] { this });
        }

        public ISet<UFile> RetrieveCompilationInputFiles()
        {
            if (sourceFiles == null)
            {
                var collector = new SourceFilesCollector();
                sourceFiles = collector.GetCompilationInputFiles(Asset);
            }

            return sourceFiles;
        }

        private class AssetItemComparerById : IEqualityComparer<AssetItem>
        {
            public bool Equals(AssetItem x, AssetItem y)
            {
                if (ReferenceEquals(x, y))
                    return true;

                if (x == null || y == null)
                {
                    return false;
                }

                if (ReferenceEquals(x.Asset, y.Asset))
                {
                    return true;
                }

                return x.Id == y.Id;
            }

            public int GetHashCode(AssetItem obj)
            {
                return obj.Id.GetHashCode();
            }
        }
    }
}
