// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Linq;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets
{
    public interface IReadOnlyPackageCollection : IReadOnlyCollection<Package>, INotifyCollectionChanged
    {
        Package Find(Dependency dependency);

        Package Find(PackageDependency packageDependency);

        Package Find(string name, PackageVersionRange versionRange);
    }

    /// <summary>
    /// A collection of <see cref="Package"/>.
    /// </summary>
    [DebuggerTypeProxy(typeof(CollectionDebugView))]
    [DebuggerDisplay("Count = {Count}")]
    [DataContract("PackageCollection")]
    public sealed class PackageCollection : ICollection<Package>, INotifyCollectionChanged, IReadOnlyPackageCollection
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
            switch (dependency.Type)
            {
                case DependencyType.Package:
                    return Find(dependency.Name, new PackageVersionRange(dependency.Version));
                case DependencyType.Project:
                    // Project versions might not be properly loaded so we check only by name
                    return packages.FirstOrDefault(package => package.Meta.Name == dependency.Name && package.Container is SolutionProject);
                default:
                    throw new ArgumentException($"Unhandled value: {dependency.Type}");
            }
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
            packages.Add(item);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item));
        }

        public void Clear()
        {
            var oldPackages = new List<Package>(packages);
            packages.Clear();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, packages, oldPackages));
        }

        public bool Contains(Package item)
        {
            if (item == null) throw new ArgumentNullException("item");
            return packages.Contains(item);
        }

        public void CopyTo(Package[] array, int arrayIndex)
        {
            packages.CopyTo(array, arrayIndex);
        }

        public bool Remove(Package item)
        {
            if (item == null) throw new ArgumentNullException("item");
            var success = packages.Remove(item);
            if (success)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item));
            return success;
        }

        private void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }
    }
}
