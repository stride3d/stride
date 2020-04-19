// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using Stride.Core;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets
{
    /// <summary>
    /// A collection of <see cref="AssetItem"/> that contains only absolute location without any drive information. This class cannot be inherited.
    /// </summary>
    [DebuggerTypeProxy(typeof(CollectionDebugView))]
    [DebuggerDisplay("Count = {Count}")]
    public sealed class PackageAssetCollection : ICollection<AssetItem>, IReadOnlyCollection<AssetItem>, ICollection, INotifyCollectionChanged
    {
        private readonly Package package;
        private object syncRoot;

        // Maps Asset.Location to Asset.Id
        private readonly Dictionary<string, AssetId> mapPathToId;

        // Maps Asset.Id to Asset.Location
        private readonly Dictionary<AssetId, string> mapIdToPath;

        // Maps Id to AssetItem
        private readonly Dictionary<AssetId, AssetItem> mapIdToAsset;

        // All registered items
        private readonly HashSet<AssetItem> registeredItems;

        private bool collectionChangedSuspended;

        /// <summary>
        /// Occurs when the collection changes.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageAssetCollection" /> class.
        /// </summary>
        /// <param name="package">The package that will contain assets.</param>
        public PackageAssetCollection(Package package)
        {
            if (package == null) throw new ArgumentNullException("package");
            this.package = package;
            mapPathToId = new Dictionary<string, AssetId>();
            mapIdToPath = new Dictionary<AssetId, string>();
            mapIdToAsset = new Dictionary<AssetId, AssetItem>();
            registeredItems = new HashSet<AssetItem>(new ReferenceEqualityComparer<AssetItem>());
        }

        /// <summary>
        /// Gets the package this collection is attached to.
        /// </summary>
        /// <value>The package.</value>
        public Package Package
        {
            get
            {
                return package;
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is dirty. Sets this flag when moving assets between packages
        /// or removing assets.
        /// </summary>
        /// <value><c>true</c> if this instance is dirty; otherwise, <c>false</c>.</value>
        public bool IsDirty { get; set; }

        /// <summary>
        /// Determines whether this instance contains an asset with the specified identifier.
        /// </summary>
        /// <param name="assetId">The asset identifier.</param>
        /// <returns><c>true</c> if this instance contains an asset with the specified identifier; otherwise, <c>false</c>.</returns>
        public bool ContainsById(AssetId assetId)
        {
            return mapIdToAsset.ContainsKey(assetId);
        }

        /// <summary>
        /// Finds an asset by its location.
        /// </summary>
        /// <param name="location">The location of the assets.</param>
        /// <returns>AssetItem.</returns>
        public AssetItem Find(string location)
        {
            AssetId id;
            if (!mapPathToId.TryGetValue(location, out id))
            {
                return null;
            }
            return Find(id);
        }

        /// <summary>
        /// Finds an asset by its location.
        /// </summary>
        /// <param name="assetId">The asset unique identifier.</param>
        /// <returns>AssetItem.</returns>
        public AssetItem Find(AssetId assetId)
        {
            AssetItem value;
            mapIdToAsset.TryGetValue(assetId, out value);
            return value;
        }

        /// <summary>
        /// Adds an <see cref="AssetItem"/> to this instance.
        /// </summary>
        /// <param name="item">The item to add to this instance.</param>
        public void Add(AssetItem item)
        {
            // Item is already added. Just skip it
            if (registeredItems.Contains(item))
            {
                return;
            }

            // Verify item integrity
            CheckCanAdd(item);

            // Set the parent of the item to the package
            item.Package = package;

            // Add the item
            var asset = item.Asset;
            asset.IsIdLocked = true;

            // Note: we ignore name collisions if asset is not referenceable
            var referenceable = item.Asset.GetType().GetCustomAttribute<AssetDescriptionAttribute>()?.Referenceable ?? true;

            // Maintain all internal maps
            if (referenceable)
                mapPathToId.Add(item.Location, item.Id);
            mapIdToPath.Add(item.Id, item.Location);
            mapIdToAsset.Add(item.Id, item);
            registeredItems.Add(item);

            // Handle notification - insert
            if (!collectionChangedSuspended)
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
            }
        }

        /// <summary>
        /// Removes all items from this instance.
        /// </summary>
        public void Clear()
        {
            // Clean parent
            foreach (var registeredItem in registeredItems)
            {
                RemoveInternal(registeredItem);
            }

            // Unregister all items / clear all internal maps
            registeredItems.Clear();
            mapIdToPath.Clear();
            mapPathToId.Clear();
            mapIdToAsset.Clear();

            // Handle notification - clear items
            if (!collectionChangedSuspended)
            {
                CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        /// <summary>
        /// Checks this collection contains the specified asset reference, throws an exception if not found.
        /// </summary>
        /// <param name="assetItem">The asset item.</param>
        /// <exception cref="System.ArgumentNullException">assetItem</exception>
        /// <exception cref="System.Collections.Generic.KeyNotFoundException">Asset [{0}] was not found.ToFormat(assetItem)</exception>
        public bool Contains(AssetItem assetItem)
        {
            if (assetItem == null) throw new ArgumentNullException("assetItem");
            return registeredItems.Contains(assetItem);
        }

        /// <summary>
        /// Copies items to the specified array.
        /// </summary>
        /// <param name="array">The array.</param>
        /// <param name="arrayIndex">Index of the array.</param>
        public void CopyTo(AssetItem[] array, int arrayIndex)
        {
            registeredItems.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Removes an <see cref="AssetItem"/> from this instance.
        /// </summary>
        /// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1" />.</param>
        /// <returns>true if <paramref name="item" /> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false. This method also returns false if <paramref name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.</returns>
        public bool Remove(AssetItem item)
        {
            if (registeredItems.Remove(item))
            {
                // Remove from all internal maps
                registeredItems.Remove(item);
                mapIdToAsset.Remove(item.Id);
                mapIdToPath.Remove(item.Id);

                // Note: we ignore name collisions if asset is not referenceable
                var referenceable = item.Asset.GetType().GetCustomAttribute<AssetDescriptionAttribute>()?.Referenceable ?? true;
                if (referenceable)
                    mapPathToId.Remove(item.Location);

                RemoveInternal(item);

                // Handle notification - replace
                if (!collectionChangedSuspended)
                {
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Removes an <see cref="AssetItem" /> from this instance.
        /// </summary>
        /// <param name="itemId">The item identifier.</param>
        /// <returns>true if <paramref name="itemId" /> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1" />; otherwise, false. This method also returns false if <paramref name="item" /> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1" />.</returns>
        public bool RemoveById(AssetId itemId)
        {
            var item = Find(itemId);
            if (item == null)
            {
                return false;
            }
            return Remove(item);
        }

        /// <summary>
        /// Suspends the collection changed that can happen on this collection.
        /// </summary>
        public void SuspendCollectionChanged()
        {
            collectionChangedSuspended = true;
        }

        /// <summary>
        /// Resumes the collection changed that happened on this collection and fire a <see cref="NotifyCollectionChangedAction.Reset"/>
        /// </summary>
        public void ResumeCollectionChanged()
        {
            collectionChangedSuspended = false;

            // Handle notification - clear items
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        private void RemoveInternal(AssetItem item)
        {
            item.Asset.IsIdLocked = false;
        }

        /// <summary>
        /// Gets the number of elements contained in this instance.
        /// </summary>
        /// <returns>The number of elements contained in this instance.</returns>
        public int Count
        {
            get
            {
                return registeredItems.Count;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this collection is read-only. Default is false.
        /// </summary>
        /// <returns>false</returns>
        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the specified item can be add to this collection.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <exception cref="System.ArgumentNullException">
        /// item;Cannot add an empty asset item reference
        /// or
        /// item;Cannot add an item with an empty asset
        /// or
        /// item;Cannot add an asset with an empty Id
        /// or
        /// item;Location cannot be null when adding an asset reference
        /// </exception>
        /// <exception cref="System.ArgumentException">
        /// An asset with the same location is already registered [{0}].ToFormat(location.Path);item
        /// or
        /// An asset with the same id [{0}] is already registered with the location [{1}].ToFormat(item.Id, location.Path);item
        /// or
        /// Asset location [{0}] cannot contain drive information.ToFormat(location);item
        /// or
        /// Asset location [{0}] must be relative and not absolute (not start with '/').ToFormat(location);item
        /// or
        /// Asset location [{0}] cannot start with relative '..'.ToFormat(location);item
        /// </exception>
        public void CheckCanAdd(AssetItem item)
        {
            // TODO better handle interaction
            if (item == null)
            {
                throw new ArgumentNullException("item", "Cannot add an empty asset item reference");
            }

            if (registeredItems.Contains(item))
            {
                throw new ArgumentException("Asset already exist in this collection", "item");
            }

            if (item.Id == AssetId.Empty)
            {
                throw new ArgumentException("Cannot add an asset with an empty Id", "item");
            }

            if (item.Package != null && item.Package != package)
            {
                throw new ArgumentException("Cannot add an asset that is already added to another package", "item");
            }

            // Note: we ignore name collisions if asset is not referenceable
            var referenceable = item.Asset.GetType().GetCustomAttribute<AssetDescriptionAttribute>()?.Referenceable ?? true;

            var location = item.Location;
            if (referenceable && mapPathToId.ContainsKey(location))
            {
                throw new ArgumentException("An asset [{0}] with the same location [{1}] is already registered ".ToFormat(mapPathToId[location], location.GetDirectoryAndFileName()), "item");
            }

            if (mapIdToPath.ContainsKey(item.Id))
            {
                throw new ArgumentException("An asset with the same id [{0}] is already registered with the location [{1}]".ToFormat(item.Id, location.GetDirectoryAndFileName()), "item");
            }

            if (location.HasDrive)
            {
                throw new ArgumentException("Asset location [{0}] cannot contain drive information".ToFormat(location), "item");
            }

            if (location.IsAbsolute)
            {
                throw new ArgumentException("Asset location [{0}] must be relative and not absolute (not start with '/')".ToFormat(location), "item");
            }

            if (location.GetDirectory() != null && location.GetDirectory().StartsWith(".."))
            {
                throw new ArgumentException("Asset location [{0}] cannot start with relative '..'".ToFormat(location), "item");
            }

            // Double check that this asset is not already stored in another package for this session
            if (Package.Session != null)
            {
                foreach (var otherPackage in Package.Session.Packages)
                {
                    if (otherPackage != Package)
                    {
                        if (otherPackage.Assets.ContainsById(item.Id))
                        {
                            throw new ArgumentException("Cannot add the asset [{0}] that is already in different package [{1}] in the current session".ToFormat(item.Id, otherPackage.Meta.Name));
                        }
                    }
                }
            }
        }

        public IEnumerator<AssetItem> GetEnumerator()
        {
            return registeredItems.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }


        void ICollection.CopyTo(Array array, int index)
        {
            foreach (var item in this)
            {
                array.SetValue(item, index++);
            }
        }

        object ICollection.SyncRoot
        {
            get
            {
                if (syncRoot == null)
                    Interlocked.CompareExchange<object>(ref syncRoot, new object(), (object)null);
                return this.syncRoot;
            }
        }

        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }
    }
}
