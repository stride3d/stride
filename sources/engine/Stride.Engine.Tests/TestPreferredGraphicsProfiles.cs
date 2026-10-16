// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Graphics;
using Xunit;
using static Stride.Graphics.GraphicsProfile;

namespace Stride.Engine.Tests
{
    public class TestPreferredGraphicsProfiles
    {
        private static readonly GraphicsProfile[] Preferred = [Level_11_1, Level_11_0, Level_10_1, Level_10_0, Level_9_3, Level_9_2, Level_9_1];

        [Fact]
        public void ProjectProfileIsTheLowestLevelTried()
        {
            Assert.Equal([Level_11_1, Level_11_0, Level_10_1, Level_10_0], Game.PreferredProfilesFrom(Preferred, Level_10_0));
        }

        [Fact]
        public void LowestProjectProfileKeepsEveryLevel()
        {
            Assert.Equal(Preferred, Game.PreferredProfilesFrom(Preferred, Level_9_1));
        }

        [Fact]
        public void ProjectProfileAboveEveryPreferredLevelIsTriedAlone()
        {
            Assert.Equal([Level_11_2], Game.PreferredProfilesFrom(Preferred, Level_11_2));
        }
    }
}
