// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace Stride.Core.Assets
{
    // TODO: Use this class as a base for AssetSourceTracker and AssetDependencyManager
    /// <summary>
    /// Base class for tracking assets and executing an action on each change.
    /// </summary>
    public abstract class AssetTracker : IDisposable
    {
        private readonly PackageSession session;
        private readonly HashSet<Package> packages = new HashSet<Package>();

        protected AssetTracker(PackageSession session)
        {
            this.session = session;
        }

        protected void Start()
        {
            session.AssetDirtyChanged += Session_AssetDirtyChanged;
            session.Packages.CollectionChanged += Packages_CollectionChanged;
            foreach (var package in session.Packages)
            {
                TrackPackage(package);
            }
        }

        public void Dispose()
        {
            session.AssetDirtyChanged -= Session_AssetDirtyChanged;
            session.Packages.CollectionChanged -= Packages_CollectionChanged;
            foreach (var package in packages.ToList())
            {
                UnTrackPackage(package);
            }
        }

        /// <summary>
        /// Called when a new asset is tracked.
        /// </summary>
        /// <param name="assetItem"></param>
        public abstract void TrackAsset(AssetItem assetItem);

        /// <summary>
        /// Called when an asset changes.
        /// </summary>
        /// <param name="asset"></param>
        public abstract void NotifyAssetChanged(Asset asset);

        /// <summary>
        /// Called when an asset stop being tracked.
        /// </summary>
        /// <param name="assetItem"></param>
        public abstract void UnTrackAsset(AssetItem assetItem);

        /// <summary>
        /// This method is called when a package needs to be tracked
        /// </summary>
        /// <param name="package">The package to track.</param>
        private void TrackPackage(Package package)
        {
            if (packages.Contains(package))
                return;

            packages.Add(package);

            foreach (var asset in package.Assets)
            {
                TrackAsset(asset);
            }

            package.Assets.CollectionChanged += Assets_CollectionChanged;
        }

        /// <summary>
        /// This method is called when a package needs to be un-tracked
        /// </summary>
        /// <param name="package">The package to un-track.</param>
        private void UnTrackPackage(Package package)
        {
            if (!packages.Contains(package))
                return;

            package.Assets.CollectionChanged -= Assets_CollectionChanged;

            foreach (var asset in package.Assets)
            {
                UnTrackAsset(asset);
            }

            packages.Remove(package);
        }

        private void Session_AssetDirtyChanged(AssetItem asset, bool oldValue, bool newValue)
        {
            // TODO: Don't update the source tracker while saving (cf AssetSourceTracker)

            NotifyAssetChanged(asset.Asset);
        }

        private void Packages_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    TrackPackage((Package)e.NewItems[0]);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    UnTrackPackage((Package)e.OldItems[0]);
                    break;

                case NotifyCollectionChangedAction.Replace:
                    foreach (var oldPackage in e.OldItems.OfType<Package>())
                    {
                        UnTrackPackage(oldPackage);
                    }

                    foreach (var package in session.Packages)
                    {
                        TrackPackage(package);
                    }
                    break;
            }
        }

        private void Assets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (AssetItem assetItem in e.NewItems)
                    {
                        TrackAsset(assetItem);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (AssetItem assetItem in e.OldItems)
                    {
                        UnTrackAsset(assetItem);
                    }
                    break;
                default:
                    throw new NotSupportedException("Reset is not supported by the asset tracker.");
            }
        }
    }
}
