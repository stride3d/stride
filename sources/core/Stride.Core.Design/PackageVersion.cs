// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
// Copyright 2010-2014 Outercurve Foundation
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//    http://www.apache.org/licenses/LICENSE-2.0
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.

using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Stride.Core;

/// <summary>
/// A hybrid implementation of SemVer that supports semantic versioning as described at http://semver.org while not strictly enforcing it to
/// allow older 4-digit versioning schemes to continue working.
/// </summary>
public sealed partial class PackageVersion : IComparable, IComparable<PackageVersion>, IEquatable<PackageVersion>
{
    private const RegexOptions Flags = RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture;
#if NET7_0_OR_GREATER
    private static readonly Regex SemanticVersionRegex = GetSemanticVersionRegex();
    private static readonly Regex StrictSemanticVersionRegex = GetStrictSemanticVersionRegex();
#else
    private static readonly Regex SemanticVersionRegex = new Regex(@"^(?<Version>\d+(\s*\.\s*\d+){0,3})(?<Release>-[0-9a-z]*[\.0-9a-z-]*)?(?<BuildMetadata>\+[0-9a-z]*[\.0-9a-z-]*)?$", Flags);
    private static readonly Regex StrictSemanticVersionRegex = new Regex(@"^(?<Version>\d+(\.\d+){2})(?<Release>-[0-9a-z]*[\.0-9a-z-]*)?(?<BuildMetadata>\+[0-9a-z]*[\.0-9a-z-]*)?$", Flags);
#endif
    private readonly string originalString;

    /// <summary>
    /// Defines version 0.
    /// </summary>
    public static readonly PackageVersion Zero = Parse("0");

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageVersion"/> class.
    /// </summary>
    /// <param name="version">The version.</param>
    public PackageVersion(string version)
        : this(Parse(version))
    {
        // The constructor normalizes the version string so that it we do not need to normalize it every time we need to operate on it.
        // The original string represents the original form in which the version is represented to be used when printing.
        originalString = version;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageVersion"/> class.
    /// </summary>
    /// <param name="major">The major.</param>
    /// <param name="minor">The minor.</param>
    /// <param name="build">The build.</param>
    /// <param name="revision">The revision.</param>
    public PackageVersion(int major, int minor, int build, int revision)
        : this(new Version(major, minor, build, revision))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageVersion"/> class.
    /// </summary>
    /// <param name="major">The major.</param>
    /// <param name="minor">The minor.</param>
    /// <param name="build">The build.</param>
    /// <param name="specialVersion">The special version.</param>
    public PackageVersion(int major, int minor, int build, string specialVersion)
        : this(new Version(major, minor, build), specialVersion)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageVersion"/> class.
    /// </summary>
    /// <param name="version">The version.</param>
    public PackageVersion(Version version)
        : this(version, string.Empty)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PackageVersion"/> class.
    /// </summary>
    /// <param name="version">The version.</param>
    /// <param name="specialVersion">The special version.</param>
    public PackageVersion(Version version, string specialVersion)
        : this(version, specialVersion, null)
    {
    }

    private PackageVersion(Version version, string specialVersion, string? originalString)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(version);
#else
        if (version is null) throw new ArgumentNullException(nameof(version));
#endif
        Version = NormalizeVersionValue(version);
        SpecialVersion = specialVersion ?? string.Empty;
        this.originalString = string.IsNullOrEmpty(originalString) ? version + (!string.IsNullOrEmpty(specialVersion) ? '-' + specialVersion : null) : originalString;
    }

    internal PackageVersion(PackageVersion semVer)
    {
        originalString = semVer.ToString();
        Version = semVer.Version;
        SpecialVersion = semVer.SpecialVersion;
    }

    /// <summary>
    /// Gets the normalized version portion.
    /// </summary>
    public Version Version { get; }

    /// <summary>
    /// Gets the optional special version.
    /// </summary>
    public string SpecialVersion { get; }

    public string[] GetOriginalVersionComponents()
    {
        if (!string.IsNullOrEmpty(originalString))
        {
            // search the start of the SpecialVersion part, if any
            int dashIndex = originalString.IndexOf('-');
            var original = dashIndex != -1 ? originalString[..dashIndex] : originalString;

            return SplitAndPadVersionString(original);
        }
        else
        {
            return SplitAndPadVersionString(Version.ToString());
        }
    }

    private static string[] SplitAndPadVersionString(string version)
    {
        string[] a = version.Split('.');
        if (a.Length == 4)
        {
            return a;
        }
        else
        {
            // if 'a' has less than 4 elements, we pad the '0' at the end
            // to make it 4.
            string[] b = ["0", "0", "0", "0"];
            Array.Copy(a, 0, b, 0, a.Length);
            return b;
        }
    }

    /// <summary>
    /// Parses a version string using loose semantic versioning rules that allows 2-4 version components followed by an optional special version.
    /// </summary>
    public static PackageVersion Parse(string version)
    {
        if (string.IsNullOrEmpty(version))
        {
            throw new ArgumentNullException(nameof(version), "cannot be null or empty");
        }

        if (!TryParse(version, out var semVer))
        {
            throw new ArgumentException($"Invalid version format [{version}]", nameof(version));
        }
        return semVer;
    }

    /// <summary>
    /// Parses a version string using loose semantic versioning rules that allows 2-4 version components followed by an optional special version.
    /// </summary>
    public static bool TryParse(string version, [MaybeNullWhen(false)] out PackageVersion value)
    {
        return TryParseInternal(version, SemanticVersionRegex, out value);
    }

    /// <summary>
    /// Parses a version string using strict semantic versioning rules that allows exactly 3 components and an optional special version.
    /// </summary>
    public static bool TryParseStrict(string version, [MaybeNullWhen(false)] out PackageVersion value)
    {
        return TryParseInternal(version, StrictSemanticVersionRegex, out value);
    }

    private static bool TryParseInternal(string version, Regex regex, [MaybeNullWhen(false)] out PackageVersion semVer)
    {
        semVer = null;
        if (string.IsNullOrEmpty(version))
        {
            return false;
        }

        var match = regex.Match(version.Trim());
        if (!match.Success || !Version.TryParse(match.Groups["Version"].Value, out var versionValue))
        {
            // Support integer version numbers (i.e. 1 -> 1.0)
            if (int.TryParse(version, out var versionNumber))
            {
                semVer = new PackageVersion(new Version(versionNumber, 0));
                return true;
            }

            return false;
        }

        semVer = new PackageVersion(NormalizeVersionValue(versionValue), match.Groups["Release"].Value.TrimStart('-'), version.Replace(" ", string.Empty));
        return true;
    }

    /// <summary>
    /// Attempts to parse the version token as a SemanticVersion.
    /// </summary>
    /// <returns>An instance of SemanticVersion if it parses correctly, null otherwise.</returns>
    public static PackageVersion? ParseOptionalVersion(string version)
    {
        _ = TryParse(version, out var semVer);
        return semVer;
    }

    private static Version NormalizeVersionValue(Version version)
    {
        return new Version(version.Major,
            version.Minor,
            Math.Max(version.Build, 0),
            Math.Max(version.Revision, 0));
    }

    public int CompareTo(object? obj)
    {
        if (ReferenceEquals(obj, null))
        {
            return 1;
        }
        if (obj is not PackageVersion other)
        {
            throw new ArgumentException($"Object must be a {nameof(PackageVersion)}", nameof(obj));
        }
        return CompareTo(other);
    }

    public int CompareTo(PackageVersion? other)
    {
        if (ReferenceEquals(other, null))
        {
            return 1;
        }

        int result = Version.CompareTo(other.Version);

        if (result != 0)
        {
            return result;
        }

        bool empty = string.IsNullOrEmpty(SpecialVersion);
        bool otherEmpty = string.IsNullOrEmpty(other.SpecialVersion);
        if (empty && otherEmpty)
        {
            return 0;
        }
        else if (empty)
        {
            return 1;
        }
        else if (otherEmpty)
        {
            return -1;
        }
        return StringComparer.OrdinalIgnoreCase.Compare(SpecialVersion, other.SpecialVersion);
    }

    public static bool operator ==(PackageVersion? version1, PackageVersion? version2)
    {
        return Equals(version1, version2);
    }

    public static bool operator !=(PackageVersion? version1, PackageVersion? version2)
    {
        return !Equals(version1, version2);
    }

    public static bool operator <(PackageVersion version1, PackageVersion? version2)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(version1);
#else
        if (version1 is null) throw new ArgumentNullException(nameof(version1));
#endif
        return version1.CompareTo(version2) < 0;
    }

    public static bool operator <=(PackageVersion version1, PackageVersion? version2)
    {
        return version1 == version2 || version1 < version2;
    }

    public static bool operator >(PackageVersion version1, PackageVersion? version2)
    {
#if NET6_0_OR_GREATER
        ArgumentNullException.ThrowIfNull(version1);
#else
        if (version1 is null) throw new ArgumentNullException(nameof(version1));
#endif
        return version1.CompareTo(version2) > 0;
    }

    public static bool operator >=(PackageVersion version1, PackageVersion? version2)
    {
        return version1 == version2 || version1 > version2;
    }

    /// <inheritdoc/>
    public override string ToString()
    {
        return originalString;
    }

    /// <inheritdoc/>
    public bool Equals([NotNullWhen(true)] PackageVersion? other)
    {
        if (ReferenceEquals(other, null)) return false;
        if (ReferenceEquals(other, this)) return true;
        return Version.Equals(other.Version) &&
               SpecialVersion.Equals(other.SpecialVersion, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc/>
    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (ReferenceEquals(obj, null)) return false;
        return obj is PackageVersion version && Equals(version);
    }

    /// <inheritdoc/>
    public override int GetHashCode()
    {
#if NETCOREAPP2_1_OR_GREATER
        return HashCode.Combine(Version, SpecialVersion);
#else
        unchecked
        {
            var hashCode = Version.GetHashCode();
            if (SpecialVersion != null)
            {
                hashCode = (hashCode * 4567) ^ SpecialVersion.GetHashCode();
            }
            return hashCode;
        }
#endif
    }

#if NET7_0_OR_GREATER
    [GeneratedRegex(@"^(?<Version>\d+(\s*\.\s*\d+){0,3})(?<Release>-[0-9a-z]*[\.0-9a-z-]*)?(?<BuildMetadata>\+[0-9a-z]*[\.0-9a-z-]*)?$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture)]
    private static partial Regex GetSemanticVersionRegex();
    [GeneratedRegex(@"^(?<Version>\d+(\.\d+){2})(?<Release>-[0-9a-z]*[\.0-9a-z-]*)?(?<BuildMetadata>\+[0-9a-z]*[\.0-9a-z-]*)?$", RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture)]
    private static partial Regex GetStrictSemanticVersionRegex();
#endif
}
