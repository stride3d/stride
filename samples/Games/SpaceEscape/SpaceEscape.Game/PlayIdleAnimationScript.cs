// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Engine;

namespace SpaceEscape
{
    /// <summary>
    /// Plays the idle animation of the entity if any
    /// </summary>
    public class PlayAnimationScript : StartupScript
    {
        public string AnimationName;

        public override void Start()
        {
            var animation = Entity.Get<AnimationComponent>();
            if (animation != null)
                animation.Play(AnimationName);
        }
    }
}
