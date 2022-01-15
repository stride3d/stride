// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#pragma warning disable 436 // The type 'type' in 'assembly' conflicts with the imported type 'type2' in 'assembly' (due to StrideVersion being duplicated)
#pragma warning disable SA1402 // File may only contain a single class
#pragma warning disable SA1649 // File name must match first type name

using System.Reflection;
using Stride;

[assembly: AssemblyVersion(StrideVersion.PublicVersion)]
[assembly: AssemblyFileVersion(StrideVersion.PublicVersion)]

[assembly: AssemblyInformationalVersion(StrideVersion.AssemblyInformationalVersion)]

namespace Stride
{
    /// <summary>
    /// Internal version used to identify Stride version.
    /// </summary>
    /// <remarks>
    /// During package build, PackageUpdateVersionTask is updating that file and expect some specific text regex so be careful if you change any of this.
    /// </remarks>
    internal class StrideVersion
    {
        /// <summary>
        /// The version used by editor for display purpose. The 4th digit will automatically be replaced by the git height when building packages with Stride.Build.
        /// </summary>
        public const string PublicVersion = "4.1.0.1";

        /// <summary>
        /// The current assembly version as text, currently same as <see cref="PublicVersion"/>.
        /// </summary>
        public const string AssemblyVersion = PublicVersion;

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
        public const string NuGetVersionSuffix = "-beta";

        /// <summary>
        /// The build metadata, usually +g[git_hash] during package. Automatically set by Stride.GitVersioning.GenerateVersionFile.
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
}
