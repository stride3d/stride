// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core;

namespace Stride.Cli.Core;

// What `stride new` does for each kind of template. Item templates (e.g. the stride-pack-* asset packs) drop content
// into an existing project; project templates generate a new one.
internal static class NewTemplateRules
{
    public static bool IsItemTemplate(IReadOnlyDictionary<string, string> tags)
        => tags.TryGetValue("type", out var type) && string.Equals(type, "item", StringComparison.OrdinalIgnoreCase);

    // The version to take a template from, given the one resolved before the template was known (--version, else the
    // newest installed release). Without --version, an item template follows the project it goes into.
    public static PackageVersion ChooseVersion(string? versionSpec, bool isItemTemplate, PackageVersion resolved, Func<PackageVersion?> projectVersion)
        => isItemTemplate && versionSpec is null ? projectVersion() ?? resolved : resolved;

    // The engine version a generated project must be upgraded from to reach target, or null to leave it alone. Only a
    // project template generates the project: what an item template's output folder holds is the user's own.
    public static string? VersionToUpgradeFrom(bool isItemTemplate, PackageVersion target, Func<string?> readGeneratedVersion)
    {
        if (isItemTemplate)
            return null;

        var generated = readGeneratedVersion();
        return generated is not null && IsOlderThan(generated, target) ? generated : null;
    }

    // True if the template's engine version is semantically older than requested (so an upgrade is warranted);
    // equal or newer is left alone.
    private static bool IsOlderThan(string templateVersion, PackageVersion requested)
    {
        try
        {
            return new PackageVersion(templateVersion).CompareTo(requested) < 0;
        }
        catch
        {
            return !string.Equals(templateVersion, requested.ToString(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
