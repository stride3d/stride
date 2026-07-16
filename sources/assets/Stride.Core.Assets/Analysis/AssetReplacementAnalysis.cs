// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Diagnostics;

namespace Stride.Core.Assets.Analysis;

/// <summary>A validated <see cref="Asset.Replaces"/> declaration.</summary>
public readonly record struct AssetReplacement(AssetItem Target, AssetItem Replacement);

/// <summary>
/// Build-time support for <see cref="Asset.Replaces"/>: validates the declarations visible from the
/// root package, then substitutes each replaced asset's content in the build session so both
/// compile-time baking and the compiled content index resolve to the replacement.
/// </summary>
public static class AssetReplacementAnalysis
{
    /// <summary>
    /// Collects and validates the replacement declarations of <paramref name="rootPackage"/> and its
    /// dependencies. When two packages replace the same URL, a declaration from
    /// <paramref name="selfPackages"/> (the built game's own packages) wins over a dependency's;
    /// other conflicts, missing targets and incompatible asset types are errors.
    /// </summary>
    /// <returns>false when any declaration is invalid (errors are logged).</returns>
    public static bool TryCollect(Package rootPackage, IReadOnlySet<Package> selfPackages, ILogger logger, out List<AssetReplacement> replacements)
    {
        var replacementsByUrl = new Dictionary<string, AssetReplacement>(StringComparer.OrdinalIgnoreCase);
        var success = true;

        foreach (var package in rootPackage.GetPackagesWithDependencies())
        {
            foreach (var assetItem in package.Assets)
            {
                if (assetItem.Asset.Replaces is not { } target)
                    continue;

                var targetItem = rootPackage.FindAsset(target);
                if (ValidateDeclaration(assetItem, targetItem) is { } error)
                {
                    logger.Error(error);
                    success = false;
                    continue;
                }

                var targetUrl = targetItem!.Location.FullPath;
                if (replacementsByUrl.TryGetValue(targetUrl, out var entry))
                {
                    var existing = entry.Replacement;
                    if (existing.Id == assetItem.Id)
                        continue;
                    var existingIsSelf = existing.Package is not null && selfPackages.Contains(existing.Package);
                    var newIsSelf = assetItem.Package is not null && selfPackages.Contains(assetItem.Package);
                    if (existingIsSelf == newIsSelf)
                    {
                        logger.Error($"Assets [{existing.Location}] and [{assetItem.Location}] both replace [{targetUrl}]; only one replacement per URL is allowed.");
                        success = false;
                    }
                    else if (newIsSelf)
                    {
                        logger.Info($"Asset [{assetItem.Location}] takes precedence over [{existing.Location}] for replacing [{targetUrl}].");
                        replacementsByUrl[targetUrl] = entry with { Replacement = assetItem };
                    }
                    else
                    {
                        logger.Info($"Asset [{existing.Location}] takes precedence over [{assetItem.Location}] for replacing [{targetUrl}].");
                    }
                    continue;
                }
                replacementsByUrl.Add(targetUrl, new AssetReplacement(targetItem, assetItem));
            }
        }

        replacements = [.. replacementsByUrl.Values];
        return success;
    }

    /// <summary>
    /// Validates a single replacement declaration against its resolved target.
    /// </summary>
    /// <returns>An error message, or null when the declaration is valid.</returns>
    public static string? ValidateDeclaration(AssetItem replacer, AssetItem? target)
    {
        if (target is null)
            return $"Asset [{replacer.Location}] replaces [{replacer.Asset.Replaces}], which does not exist in the package or its dependencies.";
        if (target.Id == replacer.Id)
            return $"Asset [{replacer.Location}] cannot replace itself.";
        if (target.Asset.Replaces is not null)
            return $"Asset [{replacer.Location}] cannot replace [{target.Location}], which itself replaces [{target.Asset.Replaces}]; chained replacements are not supported.";
        if (!target.Asset.GetType().IsAssignableFrom(replacer.Asset.GetType()))
            return $"Asset [{replacer.Location}] of type [{replacer.Asset.GetType().Name}] cannot replace [{target.Location}] of type [{target.Asset.GetType().Name}].";
        if (target.Asset is SourceCodeAsset)
        {
            // Source code assets compile from their file on disk, not from the in-memory asset
            return $"Asset [{replacer.Location}] cannot replace [{target.Location}]: replacing source code assets is not supported.";
        }
        return null;
    }

    /// <summary>
    /// Collects the session's replacement declarations without validation, keyed by replaced URL
    /// (first declaration wins). For editor use, where problems surface as diagnostics instead of errors.
    /// </summary>
    public static Dictionary<string, AssetItem>? CollectSessionReplacements(PackageSession? session)
    {
        if (session is null)
            return null;

        Dictionary<string, AssetItem>? replacements = null;
        foreach (var package in session.Packages)
        {
            foreach (var item in package.Assets)
            {
                if (item.Asset.Replaces is { } target)
                {
                    replacements ??= new Dictionary<string, AssetItem>(StringComparer.OrdinalIgnoreCase);
                    replacements.TryAdd(target.FullPath, item);
                }
            }
        }
        return replacements;
    }

    /// <summary>
    /// Swaps each replaced asset's content for a clone of its replacement, keeping the replaced
    /// asset's identity (id and location) so id- and URL-based resolution — including compile-time
    /// content baking — use the replacement. For build sessions only (the swap is never saved).
    /// </summary>
    public static void Substitute(IEnumerable<AssetReplacement> replacements)
    {
        foreach (var (target, replacement) in replacements)
        {
            var clone = AssetCloner.Clone(replacement.Asset)!;
            clone.Id = target.Id;
            // Substitution changes content, not build membership: the swapped asset must not carry
            // the declaration (it would point at its own location)
            clone.Replaces = null;
            // Derived content is stored flattened, so the archetype adds nothing here — and after
            // the id stamp it would reference the clone itself
            clone.Archetype = null;
            var package = target.Package!;
            package.Assets.Remove(target);
            package.Assets.Add(new AssetItem(target.Location, clone) { SourceFolder = target.SourceFolder });
        }
    }
}
