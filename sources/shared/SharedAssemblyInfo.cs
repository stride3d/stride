// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable 436 // The type 'type' in 'assembly' conflicts with the imported type 'type2' in 'assembly' (due to StrideVersion being duplicated)
#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1649 // File name must match first type name

using System.Reflection;
using Stride;

[assembly: AssemblyVersion(StrideVersion.AssemblyVersion)]
[assembly: AssemblyFileVersion(StrideVersion.PublicVersion)]

[assembly: AssemblyInformationalVersion(StrideVersion.AssemblyInformationalVersion)]

namespace Stride;

/// <summary>
/// Internal version used to identify Stride version.
/// </summary>
/// <remarks>
/// This file is the source of truth for the version: it is the committed value, bumped per release (not derived
/// from git tags). The generators (StrideVersionTasks.cs) read <see cref="MajorMinor"/> + <see cref="Patch"/> +
/// <see cref="NuGetVersionSuffix"/> and overlay them into a single generated copy, SharedAssemblyInfo.Generated.cs,
/// which the Stride SDK swaps in at build time (adding the -devN worktree suffix on dev builds, +g&lt;sha&gt; on
/// package builds). Keep the shape of the MajorMinor/Patch/NuGetVersionSuffix/PublicVersion/BuildMetadata lines
/// (name = "value";) so the regexes match. Its <see cref="PublicVersion"/> is a sentinel (see below).
/// </remarks>
internal class StrideVersion
{
    // ── Editable inputs ──────────────────────────────────────────────────────────────────────────────────
    // The version is MajorMinor.Patch + NuGetVersionSuffix. Edit these and bump per release — usually that's
    // release.yml bumping Patch automatically after a stable release, but you edit by hand to start a new
    // major/minor cycle or a beta. (Patch is a string, not an int, only because a const string can't concatenate
    // an int.) Don't edit the derived consts below.

    /// <summary>
    /// Release line. The single source for major.minor; pins <see cref="AssemblyVersion"/>. Bump when starting a new
    /// major/minor cycle.
    /// </summary>
    public const string MajorMinor = "4.4";

    /// <summary>
    /// The patch within <see cref="MajorMinor"/>, so the version is MajorMinor.Patch. Bumped per stable release
    /// (release.yml). A patch bump is also what gates asset upgraders, so bump it whenever the asset format changes.
    /// </summary>
    public const string Patch = "0";

    /// <summary>
    /// The prerelease suffix (e.g. -beta1), a cosmetic, NuGet-ordered label — asset upgraders ignore it (they gate on
    /// the numeric version). Empty for a stable release. The generators overlay it with -devN on dev builds.
    /// </summary>
    public const string NuGetVersionSuffix = "";

    /// <summary>
    /// Base version of the content-versioned template packages (Starters, Samples), independent of the engine
    /// version. The bridge query and Stride.Templates.Common.targets pack both append <see cref="NuGetVersionSuffix"/>
    /// to it (one source, so they can't drift); StrideSamplesVersion.props reads it for the build.
    /// </summary>
    public const string SamplesVersion = "4.4.0";

    // ── Derived / overlaid ───────────────────────────────────────────────────────────────────────────────

    /// <summary>The version: MajorMinor.Patch (the single readable value; the editable parts are above).</summary>
    public const string Version = MajorMinor + "." + Patch;

    /// <summary>
    /// The build version (used for display and as <see cref="AssemblyFileVersion"/> /
    /// <see cref="AssemblyInformationalVersion"/>). The generators overlay it with the real version. In this
    /// un-overlaid template it is a deliberately implausible sentinel — decoupled from <see cref="Version"/> so
    /// a build that skipped the overlay swap ships an obvious 4.4.65534 instead of a plausible-looking version.
    /// </summary>
    public const string PublicVersion = MajorMinor + ".65534";

    /// <summary>
    /// The assembly binding identity: pinned per major.minor (the patch must not churn it), so it is derived from
    /// <see cref="MajorMinor"/> rather than from the build version.
    /// </summary>
    public const string AssemblyVersion = MajorMinor + ".0.0";

    /// <summary>
    /// The NuGet package version.
    /// </summary>
    public const string NuGetVersion = PublicVersion + NuGetVersionSuffix;

    /// <summary>
    /// The build metadata, usually +g[git_hash] during package. Set by the release generator (Stride.GitVersion.targets).
    /// </summary>
    public const string BuildMetadata = "";

    /// <summary>
    /// The informational assembly version, containing -beta01 or +g[git_hash] during package.
    /// </summary>
    public const string AssemblyInformationalVersion = PublicVersion + NuGetVersionSuffix + BuildMetadata;
}

/// <summary>
/// Assembly signing information.
/// </summary>
internal partial class PublicKeys
{
    /// <summary>
    /// Assembly name suffix that contains signing information.
    /// </summary>
#if STRIDE_SIGNED
    public const string Default = ", PublicKey=0024000004800000940000000602000000240000525341310004000001000100f5ddb3ad5749f108242f29cfaa2205e4a6b87c7444314975dc0fbed53b7d638c17f9540763e7355be932481737cd97a4104aecda872c4805ed9473c70c239d8798b22aefc351bb2cc387eb4391f31c53aeb0452b89433562b06754af8e460384656cd388fb9bbfef348292f9fb4ee6d07b74a8490923079865a60238df259cd2";
#else
    public const string Default = "";
#endif
}
