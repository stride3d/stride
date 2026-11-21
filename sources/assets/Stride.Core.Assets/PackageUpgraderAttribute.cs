// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Annotations;
using Stride.Core.Reflection;

namespace Stride.Core.Assets;

/// <summary>
/// Attribute that describes what a package upgrader can do.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[BaseTypeRequired(typeof(PackageUpgrader))]
[AssemblyScan]
public class PackageUpgraderAttribute : Attribute
{
    private readonly PackageVersionRange? updatedVersionRange;
    private string? name;

    public string[] PackageNames { get; }

    /// <summary>
    /// The identity of the upgrader, which names the record of its upgrades in a project (see
    /// <see cref="PackageUpgrader.UpgradeRecordItem"/>); the first of <see cref="PackageNames"/> when not set.
    /// </summary>
    public string Name { get => name ?? PackageNames[0]; set => name = value; }

    /// <summary>
    /// The address of the upgrade guide's section about a step, with <c>{version}</c> for the major and minor numbers of the
    /// step's gate and <c>{id}</c> for its ID in lower case, e.g. <c>https://example.com/upgrade/{version}.html#{id}</c>;
    /// <c>null</c> when the upgrader has no guide.
    /// </summary>
    public string? HelpLinkFormat { get; set; }

    public PackageVersion PackageMinimumVersion { get; }

    public PackageVersionRange? UpdatedVersionRange => updatedVersionRange;

    public PackageUpgraderAttribute(string[] packageNames, string packageMinimumVersion, string packageUpdatedVersionRange)
    {
        PackageNames = packageNames;
        PackageMinimumVersion = new PackageVersion(packageMinimumVersion);
        _ = PackageVersionRange.TryParse(packageUpdatedVersionRange, out updatedVersionRange);
    }

    /// <summary>The address of the upgrade guide's section about the step <paramref name="id"/> gated at <paramref name="gate"/>, or <c>null</c>.</summary>
    public string? GetHelpLink(string id, PackageVersion gate)
        => HelpLinkFormat?.Replace("{version}", $"{gate.Version.Major}.{gate.Version.Minor}", StringComparison.Ordinal)
            .Replace("{id}", id.ToLowerInvariant(), StringComparison.Ordinal);
}
