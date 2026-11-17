// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Cli.Core;

/// <summary>
///   The kinds of builds besides releases that the newest version or the newest of a line may be: the
///   <c>--prerelease</c>, <c>--nightly</c> and <c>--tag</c> options.
/// </summary>
/// <param name="Prerelease">Whether alpha, beta, preview and rc versions are allowed.</param>
/// <param name="Nightly">Whether nightly builds are allowed.</param>
/// <param name="Tag">
///   The tag of the builds to take, which are then the only ones taken: the custom builds of a studio (mystudio), or the
///   local builds of a Stride checkout (dev8); null for none.
/// </param>
public readonly record struct VersionFilter(bool Prerelease = false, bool Nightly = false, string? Tag = null)
{
    /// <summary>The options as they are written on the command line, such as <c>--nightly --tag mystudio</c>.</summary>
    public string Options => string.Join(" ", new[]
    {
        Prerelease ? "--prerelease" : null,
        Nightly ? "--nightly" : null,
        Tag is not null ? $"--tag {Tag}" : null,
    }.OfType<string>());
}
