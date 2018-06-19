// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Linq;

using NUnit.Framework;

using Xenko.Games;

namespace Xenko.Graphics.Regression
{
    public class TestGraphicsDeviceManager : GraphicsDeviceManager
    {
        public TestGraphicsDeviceManager(GameBase game)
            : base(game)
        {
        }

        protected override bool IsPreferredProfileAvailable(GraphicsProfile[] preferredProfiles, out GraphicsProfile availableProfile)
        {
            if(!base.IsPreferredProfileAvailable(preferredProfiles, out availableProfile))
            {
                var minimumProfile = preferredProfiles.Min();
                Assert.Ignore("This test requires the '{0}' graphic profile. It has been ignored", minimumProfile);
            }

            return true;
        }
    }
}
