// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Stride.Core;
using Stride.Launcher.ViewModels;
using Xunit;

namespace Stride.Launcher.Tests;

public sealed class VersionCaptionTests
{
    [Fact]
    public void ANightlyIsNamedByItsVersionAndItsDateGoesApart()
    {
        var caption = VersionCaption.Create("Install {0}", "Stride ", "Stride 4.4.3-nightly.20261114.60", PackageVersion.Parse("4.4.3-nightly.20261114.60"));

        Assert.Equal("Install Stride 4.4.3", caption.Text);
        Assert.Equal(new DateTime(2026, 11, 14), caption.BuildDate);
        Assert.True(caption.IsNightly);
    }

    [Theory]
    [InlineData("4.5.3-mystudio.20261114.3", "4.5.3")]
    [InlineData("4.4.3-nightly-x.20261114.60", "4.4.3")]
    public void ACustomBuildNumberedLikeANightlyHasItsDateApartButNoNightlyBadge(string version, string number)
    {
        var caption = VersionCaption.Create("Start {0}", "", version, PackageVersion.Parse(version));

        Assert.Equal($"Start {number}", caption.Text);
        Assert.Equal(new DateTime(2026, 11, 14), caption.BuildDate);
        Assert.False(caption.IsNightly);
    }

    [Theory]
    [InlineData("4.4.2")]
    [InlineData("4.5.0-dev4")]
    [InlineData("4.5.0-beta1")]
    // Another format, or a date that doesn't exist: the version is shown whole
    [InlineData("4.4.3-nightly.2026111.60")]
    [InlineData("4.5.3-mystudio.20261114")]
    [InlineData("4.4.3-nightly.20261314.60")]
    public void AnyOtherVersionIsShownWhole(string version)
    {
        var caption = VersionCaption.Create("Start {0}", "", version, PackageVersion.Parse(version));

        Assert.Equal($"Start {version}", caption.Text);
        Assert.Null(caption.BuildDate);
        Assert.False(caption.IsDated);
        Assert.False(caption.IsNightly);
    }

    [Theory]
    [InlineData("en-US", "Stride 4.5.3 · {0:MMM d}", "Stride 4.5.3 · {0:MMM d, yyyy}")]
    [InlineData("en-GB", "Stride 4.5.3 · {0:d MMM}", "Stride 4.5.3 · {0:d MMM yyyy}")]
    public void TheShortNameOfANightlyHasItsYearOnlyWhenNotTheCurrentOne(string culture, string thisYear, string anotherYear)
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo(culture);
        try
        {
            var today = DateTime.Today;
            var lastYear = today.AddYears(-1);

            Assert.Equal(string.Format(CultureInfo.CurrentCulture, thisYear, today), VersionCaption.ShortDatedName("Stride", PackageVersion.Parse($"4.5.3-nightly.{today:yyyyMMdd}.59")));
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, anotherYear, lastYear), VersionCaption.ShortDatedName("Stride", PackageVersion.Parse($"4.5.3-nightly.{lastYear:yyyyMMdd}.59")));
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, thisYear, today), VersionCaption.ShortDatedName("Stride", PackageVersion.Parse($"4.5.3-mystudio.{today:yyyyMMdd}.2")));
            Assert.Null(VersionCaption.ShortDatedName("Stride", PackageVersion.Parse("4.4.0-beta7")));
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData("en-US", "November 14, 2026")]
    [InlineData("en-GB", "14 November 2026")]
    [InlineData("fr-FR", "14 novembre 2026")]
    [InlineData("ja-JP", "2026年11月14日")]
    public void TheDateIsTheLongDateOfTheCultureWithoutTheDayOfTheWeek(string culture, string expected)
    {
        var previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo(culture);
        try
        {
            var caption = VersionCaption.Create("Start {0}", "", "", PackageVersion.Parse("4.5.3-nightly.20261114.59"));

            Assert.Equal(expected, caption.BuildDateText);
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }
}
