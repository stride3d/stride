// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

using Xunit;
using Stride.Core;

namespace Stride.Core.Design.Tests
{
    public class TestPackageVersion
    {
        [Theory]
        [InlineData("1.0.0")]
        [InlineData("1.10.100")]
        [InlineData("1.0.0-alpha", "1.0.0", "alpha")]
        [InlineData("1.0.0-beta", "1.0.0", "beta")]
        [InlineData("1.0.0-RC", "1.0.0", "RC")]
        [InlineData("1.0.0-0", "1.0.0", "0")]
        [InlineData("1.0.0-0.1.2", "1.0.0", "0.1.2")]
        [InlineData("1.0.0-alpha.1", "1.0.0", "alpha.1")]
        [InlineData("1.0.0-alpha.1.0.1", "1.0.0", "alpha.1.0.1")]
        [InlineData("1.0.0-alpha+001", "1.0.0", "alpha")]
        [InlineData("1.0.0-rc.1+001", "1.0.0", "rc.1")]
        [InlineData("1.0.0+build001", "1.0.0", "")]
        [InlineData("1.0.0.1", "1.0.0.1", "", true)]
        [InlineData("1.0.0.1-alpha", "1.0.0.1", "alpha", true)]
        [InlineData("1.0.0.1-alpha+build", "1.0.0.1", "alpha", true)]
        [InlineData("1.0.0.1+build", "1.0.0.1", "", true)]
        public void TestParseVersionsByValidVersionStrings(string versionString, string expectedVersionNumber = null, string expectedSpecialVersion = "", bool isFourDigitVersion = false)
        {
            expectedVersionNumber ??= versionString;

            Assert.True(PackageVersion.TryParse(versionString, out var pkgVersion), $"Failed to parse '{versionString}'");

            var actualVerStr = pkgVersion.Version.ToString(fieldCount: isFourDigitVersion ? 4 : 3);
            Assert.Equal(expectedVersionNumber, actualVerStr);

            Assert.Equal(expectedSpecialVersion, pkgVersion.SpecialVersion);
        }
    }
}
