// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections;
using System.Collections.Specialized;
using System.Diagnostics;
using Stride.Core.Diagnostics;

namespace Stride.Core.Assets;

public interface IReadOnlyPackageCollection : IReadOnlyCollection<Package>, INotifyCollectionChanged
{
    Package? Find(Dependency dependency);

    Package? Find(PackageDependency packageDependency);

    Package? Find(string name, PackageVersionRange versionRange);
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
    public event NotifyCollectionChangedEventHandler? CollectionChanged;

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageCollection"/> class.
    /// </summary>
    public PackageCollection()
    {
        packages = [];
    }

    public int Count => packages.Count;

    public bool IsReadOnly => false;

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
    public Package? Find(Dependency dependency)
    {
        ArgumentNullException.ThrowIfNull(dependency);
        return dependency.Type switch
        {
            DependencyType.Package => Find(dependency.Name, new PackageVersionRange(dependency.Version)),
            DependencyType.Project => packages.FirstOrDefault(package => package.Meta.Name == dependency.Name && package.Container is SolutionProject),// Project versions might not be properly loaded so we check only by name
            _ => throw new ArgumentException($"Unhandled value: {dependency.Type}"),
        };
    }

    /// <summary>
    /// Finds the a package already in this collection from the specified dependency.
    /// </summary>
    /// <param name="packageDependency">The package dependency.</param>
    /// <returns>Package.</returns>
    public Package? Find(PackageDependency packageDependency)
    {
        ArgumentNullException.ThrowIfNull(packageDependency);
        return Find(packageDependency.Name, packageDependency.Version);
    }

    /// <summary>
    /// Finds a package with the specified name and <see cref="PackageVersionRange"/>.
    /// </summary>
    /// <param name="name">The name.</param>
    /// <param name="versionRange">The version range.</param>
    /// <returns>Package.</returns>
    public Package? Find(string name, PackageVersionRange versionRange)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(versionRange);
        var filter = versionRange.ToFilter();
        return packages.FirstOrDefault(package => package.Meta.Name == name && filter(package));
    }

    public void Add(Package item)
    {
        ArgumentNullException.ThrowIfNull(item);
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
        ArgumentNullException.ThrowIfNull(item);
        return packages.Contains(item);
    }

    public void CopyTo(Package[] array, int arrayIndex)
    {
        packages.CopyTo(array, arrayIndex);
    }

    public bool Remove(Package item)
    {
        ArgumentNullException.ThrowIfNull(item);
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
