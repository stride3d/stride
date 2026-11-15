// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using Stride.Core;

namespace Stride.Launcher.ViewModels;

/// <summary>
/// The text of the Start and Install buttons: the action and the version, and for a dated build (a nightly, or a custom
/// build numbered the same way), the day it was built, shown apart so that its long version (4.5.3-nightly.20261114.59)
/// fits.
/// </summary>
/// <param name="Text">The action and the version, such as "Start 4.5.3".</param>
/// <param name="BuildDate">The day the build was made, or null when the version isn't a dated build.</param>
public sealed partial record VersionCaption(string Text, DateTime? BuildDate)
{
    /// <summary>
    /// Gets whether the version is a dated build, shown with its date under its X.Y.Z.
    /// </summary>
    public bool IsDated => BuildDate is not null;

    /// <summary>
    /// Gets whether the version is a nightly, shown with its badge.
    /// </summary>
    public bool IsNightly { get; init; }

    /// <summary>
    /// Gets whether the version is a local build, shown with its badge.
    /// </summary>
    public bool IsLocal { get; init; }

    /// <summary>
    /// Gets whether the version is a beta, shown with its badge.
    /// </summary>
    public bool IsBeta { get; init; }

    /// <summary>
    /// Gets the badge of the tag of a custom build (MYSTUDIO), or null.
    /// </summary>
    public string? Tag { get; init; }

    /// <summary>
    /// Gets the day the build was made, as a long date without the day of the week (November 14, 2026).
    /// </summary>
    public string? BuildDateText => BuildDate?.ToString(LongDateWithoutWeekday(), CultureInfo.CurrentCulture);

    /// <summary>
    /// Builds the caption of <paramref name="version"/>: a dated build (X.Y.Z-nightly.YYYYMMDD.N, X.Y.Z-mystudio.YYYYMMDD.N)
    /// is named by its X.Y.Z, and its date goes apart; the number after it only orders two builds of the same day. Any
    /// other version, a nightly of another format too, is named by <paramref name="name"/>.
    /// </summary>
    /// <param name="format">The text of the button, where {0} is the name of the version.</param>
    /// <param name="product">The product before the version of a nightly, such as "Stride ", or an empty string.</param>
    /// <param name="name">The name of a version that isn't a nightly.</param>
    /// <param name="version">The version.</param>
    /// <returns>The caption.</returns>
    public static VersionCaption Create(string format, string product, string name, PackageVersion? version)
        => TryParseDated(version, out var number, out var date, out var isNightly)
            ? new(string.Format(format, product + number), date) { IsNightly = isNightly }
            : new(string.Format(format, name), null);

    /// <summary>
    /// Gets the short name of a dated build in the version list, where its row has little room: its X.Y.Z and its date,
    /// the year only when it isn't the current one (Stride 4.5.3 · Nov 14).
    /// </summary>
    /// <param name="product">The product before the version, such as "Stride".</param>
    /// <param name="version">The version.</param>
    /// <returns>The short name, or null when <paramref name="version"/> isn't a dated build.</returns>
    public static string? ShortDatedName(string product, PackageVersion? version)
    {
        if (!TryParseDated(version, out var number, out var date, out _))
            return null;
        var format = CultureInfo.CurrentCulture.DateTimeFormat;
        var pattern = date.Year == DateTime.Today.Year ? format.MonthDayPattern : LongDateWithoutWeekday();
        // The abbreviated month, as Nov rather than November
        return $"{product} {number} · {date.ToString(pattern.Replace("MMMM", "MMM"), CultureInfo.CurrentCulture)}";
    }

    /// <summary>
    /// Splits a dated build, a nightly of the nightly workflow (X.Y.Z-nightly.YYYYMMDD.N) or a custom build numbered the
    /// same way (X.Y.Z-mystudio.YYYYMMDD.N), into its X.Y.Z and its date.
    /// </summary>
    private static bool TryParseDated(PackageVersion? version, [NotNullWhen(true)] out string? number, out DateTime date, out bool isNightly)
    {
        if (version?.SpecialVersion is { } label && DatedLabel().Match(label) is { Success: true } match
            && DateTime.TryParseExact(match.Groups[2].Value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
        {
            number = version.Version.ToString(3);
            isNightly = match.Groups[1].Value.Equals(StrideVersionViewModel.NightlyChannel, StringComparison.OrdinalIgnoreCase);
            return true;
        }
        number = null;
        date = default;
        isNightly = false;
        return false;
    }

    /// <summary>
    /// The long date pattern of the current culture, without its day of the week: "dddd, MMMM d, yyyy" gives "MMMM d, yyyy".
    /// </summary>
    private static string LongDateWithoutWeekday()
        => WeekdayPattern().Replace(CultureInfo.CurrentCulture.DateTimeFormat.LongDatePattern, " ").Trim();

    // The tag, then the date and the number of the build of that day
    [GeneratedRegex(@"^([0-9A-Za-z-]+)\.(\d{8})\.\d+$")]
    private static partial Regex DatedLabel();

    [GeneratedRegex(@"\s*dddd[,.]?\s*")]
    private static partial Regex WeekdayPattern();
}
