// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Xenko.Animations;
using Xenko.Engine;
using Xenko.Rendering.Sprites;

namespace PhysicsSample
{
    /// <summary>
    /// This simple script will start the sprite idle animation
    /// </summary>
    public class EnemyScript : StartupScript
    {
        public override void Start()
        {
            var spriteComponent = Entity.Get<SpriteComponent>();
            var sheet = ((SpriteFromSheet)spriteComponent.SpriteProvider).Sheet;
            SpriteAnimation.Play(spriteComponent, sheet.FindImageIndex("active0"), sheet.FindImageIndex("active1"), AnimationRepeatMode.LoopInfinite, 2);
        }
    }
}
