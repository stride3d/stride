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
/// The StrideGitVersion task (Stride.build) and Stride.WorktreeVersion.targets patch this file via regex, so be careful if you change the shape of these lines.
/// </remarks>
internal class StrideVersion
{
    /// <summary>
    /// The version used by editor for display purpose. The 3rd digit is the git height, set automatically for release packages (StrideGitVersion) and for dev builds (last release tag + 1; override via StridePublicVersion in build/Stride.Local.props).
    /// </summary>
    public const string PublicVersion = "4.4.0";

    /// <summary>
    /// The assembly binding identity: pinned per major.minor (git height must not churn it). Bump together with <see cref="PublicVersion"/>.
    /// </summary>
    public const string AssemblyVersion = "4.4.0.0";

    /// <summary>
    /// The NuGet package version without special tags.
    /// </summary>
    public const string NuGetVersionSimple = PublicVersion;

    /// <summary>
    /// The NuGet package version.
    /// </summary>
    public const string NuGetVersion = NuGetVersionSimple + NuGetVersionSuffix;

    /// <summary>
    /// The NuGet package suffix (i.e. -beta).
    /// </summary>
    public const string NuGetVersionSuffix = "";

    /// <summary>
    /// The build metadata, usually +g[git_hash] during package. Automatically set by the StrideGitVersion task and dev builds.
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
