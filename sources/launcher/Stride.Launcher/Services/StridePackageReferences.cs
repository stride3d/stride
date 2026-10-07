// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Stride.Core.Packages;

namespace Stride.Launcher.Services;

/// <summary>
/// The installed Stride packages that Stride versions use: for the cleanup after an uninstall or an update, and for
/// the removal of the versions when the launcher itself is uninstalled.
/// </summary>
internal static class StridePackageReferences
{
    public static IEqualityComparer<NugetLocalPackage> Comparer { get; } = new PackageComparer();

    /// <summary>
    /// The installed Stride packages that <paramref name="mainPackages"/> use, directly or not. The main packages
    /// themselves are not in it, unless another one uses them.
    /// </summary>
    public static HashSet<NugetLocalPackage> Find(NugetStore store, IEnumerable<NugetLocalPackage> mainPackages)
        => Find(mainPackages, InstalledDependencies(store), Comparer);

    /// <summary>
    /// The packages to remove with <paramref name="releases"/>: themselves first, then the packages they use that
    /// <paramref name="localBuilds"/> don't use. Local builds are never removed.
    /// </summary>
    public static List<NugetLocalPackage> FindRemovable(NugetStore store, IReadOnlyCollection<NugetLocalPackage> releases, IReadOnlyCollection<NugetLocalPackage> localBuilds)
        => FindRemovable(releases, localBuilds, InstalledDependencies(store), Comparer);

    internal static HashSet<T> Find<T>(IEnumerable<T> mainPackages, Func<T, IEnumerable<T>> dependencies, IEqualityComparer<T> comparer)
    {
        var referenced = new HashSet<T>(comparer);
        var pending = new Stack<T>(mainPackages);
        while (pending.TryPop(out var package))
        {
            foreach (var dependency in dependencies(package))
            {
                if (referenced.Add(dependency))
                    pending.Push(dependency);
            }
        }
        return referenced;
    }

    internal static List<T> FindRemovable<T>(IReadOnlyCollection<T> releases, IReadOnlyCollection<T> localBuilds, Func<T, IEnumerable<T>> dependencies, IEqualityComparer<T> comparer)
    {
        var kept = Find(localBuilds, dependencies, comparer);
        kept.UnionWith(localBuilds);
        var added = new HashSet<T>(comparer);
        return [.. releases.Concat(Find(releases, dependencies, comparer)).Where(package => !kept.Contains(package) && added.Add(package))];
    }

    // The installed Stride dependencies of a package. Lookups are kept per dependency (id and version range): the
    // installed versions share most of them, and each store lookup reads every installed version of the id.
    private static Func<NugetLocalPackage, IEnumerable<NugetLocalPackage>> InstalledDependencies(NugetStore store)
    {
        var lookups = new Dictionary<string, NugetLocalPackage?>();
        return package => package.Dependencies
            .Where(dependency => dependency.Item1.Split('.', 2)[0] is "Stride")
            .Select(dependency =>
            {
                var key = $"{dependency.Item1}/{dependency.Item2}";
                if (!lookups.TryGetValue(key, out var found))
                    lookups[key] = found = store.FindLocalPackage(dependency.Item1, dependency.Item2);
                return found;
            })
            .OfType<NugetLocalPackage>();
    }

    private sealed class PackageComparer : IEqualityComparer<NugetLocalPackage>
    {
        public bool Equals(NugetLocalPackage? x, NugetLocalPackage? y)
            => ReferenceEquals(x, y) || (x is not null && y is not null && x.Id == y.Id && x.Version.ToString() == y.Version.ToString());

        public int GetHashCode([DisallowNull] NugetLocalPackage obj)
            => obj.Id.GetHashCode() ^ obj.Version.ToString().GetHashCode();
    }
}
