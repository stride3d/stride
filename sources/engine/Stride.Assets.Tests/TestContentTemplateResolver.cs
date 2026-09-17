// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#nullable enable
using Stride.Assets.Templates;
using Stride.Core;
using Xunit;

namespace Stride.Assets.Tests
{
    /// <summary>
    /// The exact-version rules shared by the Game Studio bridge and the stride CLI to pick a content template package.
    /// </summary>
    public class TestContentTemplateResolver
    {
        [Theory]
        [InlineData("4.4.0-dev4", "dev4")]
        [InlineData("4.4.0-dev", "dev")]
        [InlineData("4.4.0-beta8-dev4", "dev4")]
        [InlineData("4.4.0", null)]
        [InlineData("4.4.0-beta8", null)]
        [InlineData("4.4.0-devices1", null)]
        public void DevSuffixOf(string host, string? expected)
            => Assert.Equal(expected, ContentTemplateResolver.DevSuffixOf(new PackageVersion(host)));

        [Theory]
        [InlineData("4.4.0-beta7", "4.4.0-dev4", "4.4.0-beta7-dev4")]
        [InlineData("4.4.0", "4.4.0-beta8-dev2", "4.4.0-dev2")]
        [InlineData("4.4.0", "4.4.0-beta8", null)]
        public void DevPackVersion(string content, string host, string? expected)
            => Assert.Equal(expected is null ? null : new PackageVersion(expected), ContentTemplateResolver.DevPackVersion(new PackageVersion(content), new PackageVersion(host)));

        [Theory]
        [InlineData("4.4.0-beta7-dev4", "4.4.0-dev4", "4.4.0-beta7")]
        [InlineData("4.4.0-dev4", "4.4.0-beta8-dev4", "4.4.0")]
        [InlineData("4.4.0-beta7", "4.4.0-dev4", "4.4.0-beta7")]
        [InlineData("4.4.0-beta7-dev2", "4.4.0-dev4", "4.4.0-beta7-dev2")]
        [InlineData("4.4.1", "4.4.1", "4.4.1")]
        public void ContentVersionFromPin(string pinned, string host, string expected)
            => Assert.Equal(new PackageVersion(expected), ContentTemplateResolver.ContentVersionFromPin(new PackageVersion(pinned), new PackageVersion(host)));

        [Theory]
        [InlineData("4.4.0-beta7", true)]
        [InlineData("4.4.0-beta7-dev4", true)]
        [InlineData("4.4.0-beta7-dev3", false)]
        [InlineData("4.4.0-dev4", false)]
        [InlineData("4.4.1-dev4", false)]
        [InlineData("4.4.0", false)]
        public void IsAcceptableForDevHost(string candidate, bool expected)
            => Assert.Equal(expected, ContentTemplateResolver.IsAcceptable(new PackageVersion(candidate), new PackageVersion("4.4.0-beta7"), new PackageVersion("4.4.0-dev4")));

        [Theory]
        [InlineData("4.4.0", true)]
        [InlineData("4.4.0-dev4", false)]
        [InlineData("4.4.1", false)]
        public void IsAcceptableForReleaseHost(string candidate, bool expected)
            => Assert.Equal(expected, ContentTemplateResolver.IsAcceptable(new PackageVersion(candidate), new PackageVersion("4.4.0"), new PackageVersion("4.4.0")));

        [Fact]
        public void LocalBuildRange()
        {
            // A dev host takes its own pack from local feeds, a release host the plain version; both exactly.
            AssertExactRange("4.4.0-beta7-dev4", ContentTemplateResolver.LocalBuildRange(new PackageVersion("4.4.0-beta7"), new PackageVersion("4.4.0-dev4")));
            AssertExactRange("4.4.0", ContentTemplateResolver.LocalBuildRange(new PackageVersion("4.4.0"), new PackageVersion("4.4.0")));
        }

        private static void AssertExactRange(string version, PackageVersionRange range)
        {
            Assert.Equal(new PackageVersion(version), range.MinVersion);
            Assert.Equal(new PackageVersion(version), range.MaxVersion);
            Assert.True(range.IsMinInclusive);
            Assert.True(range.IsMaxInclusive);
        }
    }
}
