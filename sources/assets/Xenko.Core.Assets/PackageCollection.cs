// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using NuGet.ProjectModel;
using Xenko.Core;
using Xenko.Core.Diagnostics;

namespace Xenko.Core.Assets
{
    /// <summary>
    /// A collection of <see cref="Package"/>.
    /// </summary>
    [DebuggerTypeProxy(typeof(CollectionDebugView))]
    [DebuggerDisplay("Count = {Count}")]
    [DataContract("PackageCollection")]
    public sealed class PackageCollection : ICollection<Package>, INotifyCollectionChanged
    {
        private readonly List<Package> packages;

        /// <summary>
        /// Occurs when the collection changes.
        /// </summary>
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageCollection"/> class.
        /// </summary>
        public PackageCollection()
        {
            packages = new List<Package>();
        }

        public int Count
        {
            get
            {
                return packages.Count;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public IEnumerator<Package> GetEnumerator()
        {
            return packages.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Finds the a package already in this collection from the specified dependency.
        /// </summary>
        /// <param name="packageDependency">The package dependency.</param>
        /// <returns>Package.</returns>
        public Package Find(Dependency dependency)
        {
            if (dependency == null) throw new ArgumentNullException(nameof(dependency));
            return Find(dependency.Name, dependency.VersionRange);
        }

        /// <summary>
        /// Finds the specified package by its unique identifier.
        /// </summary>
        /// <param name="packageGuid">The package unique identifier.</param>
        /// <returns>Package.</returns>
        public Package Find(Guid packageGuid)
        {
            return packages.FirstOrDefault(package => package.Id == packageGuid);
        }

        /// <summary>
        /// Finds the a package already in this collection from the specified dependency.
        /// </summary>
        /// <param name="packageDependency">The package dependency.</param>
        /// <returns>Package.</returns>
        public Package Find(PackageDependency packageDependency)
        {
            if (packageDependency == null) throw new ArgumentNullException("packageDependency");
            return Find(packageDependency.Name, packageDependency.Version);
        }

        /// <summary>
        /// Finds a package with the specified name and <see cref="PackageVersionRange"/>.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="versionRange">The version range.</param>
        /// <returns>Package.</returns>
        public Package Find(string name, PackageVersionRange versionRange)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (versionRange == null) throw new ArgumentNullException("versionRange");
            var filter = versionRange.ToFilter();
            return packages.FirstOrDefault(package => package.Meta.Name == name && filter(package));
        }

        public void Add(Package item)
        {
            if (item == null) throw new ArgumentNullException("item");
            if (Find(item.Id) == null)
            {
                packages.Add(item);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
            }
        }

        public void Clear()
        {
            var oldPackages = new List<Package>(packages);
            packages.Clear();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, packages, oldPackages));
        }

        /// <summary>
        /// Determines whether this collection contains a package with the specified package unique identifier.
        /// </summary>
        /// <param name="packageGuid">The package unique identifier.</param>
        /// <returns><c>true</c> if this collection contains a package with the specified package unique identifier; otherwise, <c>false</c>.</returns>
        public bool ContainsById(Guid packageGuid)
        {
            return packages.Any(package => package.Id == packageGuid);
        }

        public bool Contains(Package item)
        {
            if (item == null) throw new ArgumentNullException("item");
            return ContainsById(item.Id);
        }

        public void CopyTo(Package[] array, int arrayIndex)
        {
            packages.CopyTo(array, arrayIndex);
        }

        public bool RemoveById(Guid packageGuid)
        {
            var item = Find(packageGuid);
            if (item != null)
            {

                packages.Remove(item);
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
                return true;
            }
            return false;
        }

        public bool Remove(Package item)
        {
            if (item == null) throw new ArgumentNullException("item");
            return RemoveById(item.Id);
        }

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }
    }
}
