// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

using Xunit;
using Stride.Animations;
using Stride.Games;
using Stride.Graphics;
using Stride.Rendering.Sprites;

namespace Stride.Engine.Tests
{
    public class SpriteAnimationTest : Game
    {
        [Fact]
        public void DefaultValues()
        {
            Assert.Equal(30, SpriteAnimation.DefaultFramesPerSecond);
        }

        [Fact]
        public void TestPauseResume()
        {
            var spriteComp = CreateSpriteComponent(15);

            SpriteAnimation.Play(spriteComp, 0, 10, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Pause(spriteComp);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2)));

            Assert.Equal(0, spriteComp.CurrentFrame);

            SpriteAnimation.Play(spriteComp, 0, 10, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2)));

            Assert.Equal(2, spriteComp.CurrentFrame);
            
            SpriteAnimation.Pause(spriteComp);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2)));

            Assert.Equal(2, spriteComp.CurrentFrame);

            SpriteAnimation.Resume(spriteComp);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2)));

            Assert.Equal(4, spriteComp.CurrentFrame);
        }

        [Fact]
        public void TestStop()
        {
            var spriteComp = CreateSpriteComponent(20);

            SpriteAnimation.Play(spriteComp, 0, 1, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));

            Assert.Equal(1, spriteComp.CurrentFrame); // check that is it correctly updated by default

            SpriteAnimation.Stop(spriteComp);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));

            Assert.Equal(1, spriteComp.CurrentFrame); // check that current frame does not increase any more

            SpriteAnimation.Play(spriteComp, 2, 3, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0)));

            Assert.Equal(2, spriteComp.CurrentFrame); // check that frame is correctly set to first animation frame

            SpriteAnimation.Play(spriteComp, 2, 3, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Queue(spriteComp, 5, 6, AnimationRepeatMode.PlayOnce, 1);

            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0)));

            Assert.Equal(2, spriteComp.CurrentFrame); // check that is it correctly updated by default

            SpriteAnimation.Stop(spriteComp);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2)));

            Assert.Equal(2, spriteComp.CurrentFrame); // check that queue is correctly reset

            SpriteAnimation.Play(spriteComp, 2, 3, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3)));

            Assert.Equal(3, spriteComp.CurrentFrame); // check that queue is correctly reset

            SpriteAnimation.Stop(spriteComp);
            SpriteAnimation.Queue(spriteComp, 5, 6, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0)));

            Assert.Equal(5, spriteComp.CurrentFrame); // check that indices are correctly reseted during stop
        }

        [Fact]
        public void TestPlay()
        {
            var spriteComp = CreateSpriteComponent(20);

            SpriteAnimation.Play(spriteComp, 1, 5, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0)));

            Assert.Equal(1, spriteComp.CurrentFrame);

            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));

            Assert.Equal(2, spriteComp.CurrentFrame);

            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5)));

            Assert.Equal(2, spriteComp.CurrentFrame);

            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(9), TimeSpan.FromSeconds(9)));

            Assert.Equal(5, spriteComp.CurrentFrame); // check that it does not exceed last frame

            SpriteAnimation.Play(spriteComp, 5, 7, AnimationRepeatMode.LoopInfinite, 1);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0)));

            Assert.Equal(5, spriteComp.CurrentFrame);

            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(3)));

            Assert.Equal(5, spriteComp.CurrentFrame); // check looping

            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(4)));

            Assert.Equal(6, spriteComp.CurrentFrame); // check looping

            SpriteAnimation.Play(spriteComp, new[] { 9, 5, 10, 9 }, AnimationRepeatMode.LoopInfinite, 1);

            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0)));
            Assert.Equal(9, spriteComp.CurrentFrame);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));
            Assert.Equal(5, spriteComp.CurrentFrame);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));
            Assert.Equal(10, spriteComp.CurrentFrame);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));
            Assert.Equal(9, spriteComp.CurrentFrame);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));
            Assert.Equal(9, spriteComp.CurrentFrame);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));
            Assert.Equal(5, spriteComp.CurrentFrame);

            // check queue reset
            SpriteAnimation.Queue(spriteComp, 1, 2, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Queue(spriteComp, 3, 4, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Play(spriteComp, 7, 8, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(4)));

            Assert.Equal(8, spriteComp.CurrentFrame); // check queue reset

            SpriteAnimation.Queue(spriteComp, 1, 2, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Queue(spriteComp, 3, 4, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Play(spriteComp, new[] { 7, 8 }, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(4)));

            Assert.Equal(8, spriteComp.CurrentFrame); // check queue reset

            SpriteAnimation.Play(spriteComp, 0, 0, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Queue(spriteComp, 1, 2, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Queue(spriteComp, 3, 4, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Play(spriteComp, 7, 8, AnimationRepeatMode.PlayOnce, 1, false);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10)));

            Assert.Equal(4, spriteComp.CurrentFrame); // check queue no reset

            SpriteAnimation.Play(spriteComp, 0, 0, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Queue(spriteComp, 1, 2, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Queue(spriteComp, 3, 4, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Play(spriteComp, new[] { 7, 8 }, AnimationRepeatMode.PlayOnce, 1, false);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10)));

            Assert.Equal(4, spriteComp.CurrentFrame); // check queue no reset

            // check default fps speed
            SpriteAnimation.Play(spriteComp, 0, 15, AnimationRepeatMode.PlayOnce);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(0.51), TimeSpan.FromSeconds(0.51)));
            Assert.Equal(15, spriteComp.CurrentFrame); // check queue no reset
        }

        [Fact]
        public void TestQueue()
        {
            var spriteComp = CreateSpriteComponent(20);

            // check queue before play
            SpriteAnimation.Queue(spriteComp, 1, 3, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(0), TimeSpan.FromSeconds(0)));

            Assert.Equal(1, spriteComp.CurrentFrame);

            // check queue sequence
            SpriteAnimation.Queue(spriteComp, new[] { 5, 9, 4 }, AnimationRepeatMode.PlayOnce, 1);
            SpriteAnimation.Queue(spriteComp, new[] { 6 }, AnimationRepeatMode.LoopInfinite, 1);
            SpriteAnimation.Queue(spriteComp, new[] { 7 }, AnimationRepeatMode.PlayOnce, 1);

            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2)));
            Assert.Equal(3, spriteComp.CurrentFrame);

            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));
            Assert.Equal(5, spriteComp.CurrentFrame);

            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(2)));
            Assert.Equal(4, spriteComp.CurrentFrame);

            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1)));
            Assert.Equal(6, spriteComp.CurrentFrame);

            SpriteAnimation.Draw(new GameTime(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10)));
            Assert.Equal(6, spriteComp.CurrentFrame); 
        }

        private static SpriteComponent CreateSpriteComponent(int nbOfFrames)
        {
            var spriteGroup = new SpriteSheet();
            var sprite = new SpriteComponent { SpriteProvider = new SpriteFromSheet { Sheet = spriteGroup } };

            // add a few sprites
            for (int i = 0; i < nbOfFrames; i++)
            {
                spriteGroup.Sprites.Add(new Sprite(Guid.NewGuid().ToString()));
            }

            return sprite;
        }
    }
}
