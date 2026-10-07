// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Extensions;

namespace Stride.Core.Assets;

/// <summary>
/// The types a package can use: those of its own assemblies, of the packages it depends on (companions included) and of
/// the host.
/// </summary>
public sealed class PackageTypeScope
{
    // The session packages that loaded each assembly
    private readonly Dictionary<Assembly, List<Package>> assemblyPackages = [];
    private readonly HashSet<Package> packages = [];

    private PackageTypeScope()
    {
    }

    /// <summary>
    /// A snapshot of the scope of <paramref name="package"/>; everything is in scope for a package outside a session.
    /// </summary>
    public static PackageTypeScope For(Package package)
    {
        var scope = new PackageTypeScope();
        IEnumerable<Package> sessionPackages = package.Session?.Packages ?? (IEnumerable<Package>)[];
        foreach (var sessionPackage in sessionPackages)
        {
            foreach (var assembly in sessionPackage.LoadedAssemblies.Select(x => x.Assembly).NotNull())
            {
                if (!scope.assemblyPackages.TryGetValue(assembly, out var owners))
                    scope.assemblyPackages[assembly] = owners = [];
                owners.Add(sessionPackage);
            }
        }
        scope.packages.Add(package);
        if (package.Container is { } container)
            scope.packages.UnionWith(container.FlattenedDependencies.Select(x => x.Package).NotNull());
        return scope;
    }

    /// <summary>
    /// Whether a package in scope loaded the assembly of <paramref name="type"/>; an assembly no package loaded (the host's) always is.
    /// </summary>
    public bool Contains(Type type) => Contains(type.Assembly);

    /// <inheritdoc cref="Contains(Type)"/>
    public bool Contains(Assembly assembly)
        => !assemblyPackages.TryGetValue(assembly, out var owners) || owners.Any(packages.Contains);
}
