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
/// The version generators read <see cref="MajorMinor"/> + <see cref="MinPatch"/> from this file via regex and
/// overlay the computed version into a generated copy (Stride.WorktreeVersion.targets -> SharedAssemblyInfo.Worktree.cs
/// for dev builds, Stride.GitVersion.targets -> SharedAssemblyInfo.NuGet.cs for release/package builds). Keep the
/// shape of the MajorMinor/MinPatch/NuGetVersionSuffix/BuildMetadata lines (name = "value";) so the regexes match.
/// </remarks>
internal class StrideVersion
{
    // ── Editable inputs ──────────────────────────────────────────────────────────────────────────────────
    // The generators compute the build version as
    //     max(MinVersion, latest releases/<MajorMinor>.* + 1, [local StridePublicVersion override])
    // and overlay it back by rewriting MinPatch. Edit MajorMinor / MinPatch (the floor) — not the derived
    // PublicVersion. (MinPatch is a string, not an int, only because a const string can't concatenate an int.)

    /// <summary>
    /// Release line. Scopes the releases/&lt;MajorMinor&gt;.* tag search the generators use, and pins
    /// <see cref="AssemblyVersion"/>. The single source for major.minor. Bump when starting a new major/minor cycle.
    /// </summary>
    public const string MajorMinor = "4.4";

    /// <summary>
    /// The floor patch within <see cref="MajorMinor"/>, so the floor version is MajorMinor.MinPatch. Bump it to
    /// anchor an unreleased version before its release tag exists (e.g. for incremental asset upgraders); a higher
    /// reachable release tag overrides it automatically. Override locally (within MajorMinor) via StridePublicVersion
    /// in build/Stride.Local.props. The generators overlay this with the computed patch.
    /// </summary>
    public const string MinPatch = "0";

    /// <summary>
    /// The NuGet package suffix (i.e. -beta). The generators overlay this with -devN (dev) or the release suffix.
    /// </summary>
    public const string NuGetVersionSuffix = "";

    // ── Derived / overlaid ───────────────────────────────────────────────────────────────────────────────

    /// <summary>The minimum (floor) version: MajorMinor.MinPatch.</summary>
    public const string MinVersion = MajorMinor + "." + MinPatch;

    /// <summary>
    /// The version used by the editor for display. Equals the build version — the generators overlay
    /// <see cref="MinPatch"/> with the computed patch; mirrors <see cref="MinVersion"/> when compiled directly
    /// (CI / no overlay).
    /// </summary>
    public const string PublicVersion = MinVersion;

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
