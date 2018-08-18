// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Collections.Generic;
using Xenko.Animations;
using Xenko.Core;
using Xenko.Engine;
using Xenko.Games;

namespace Xenko.Rendering.Sprites
{
    /// <summary>
    /// A system in charge of animating the sprites 
    /// </summary>
    public class SpriteAnimationSystem : GameSystemBase
    {
        private readonly HashSet<SpriteComponent> playingSprites = new HashSet<SpriteComponent>();
        private readonly HashSet<SpriteComponent> spritesToStop = new HashSet<SpriteComponent>(); 

        /// <summary>
        /// Gets or sets the default sprite animation FPS (Default value = 30 FPS). 
        /// </summary>
        public float DefaultFramesPerSecond { get; set; }

        /// <summary>
        /// Creates a new instance of <see cref="SpriteAnimationSystem"/> and register it in the services.
        /// </summary>
        /// <param name="registry"></param>
        public SpriteAnimationSystem(IServiceRegistry registry)
            : base(registry)
        {
            DefaultFramesPerSecond = 30;
        }

        public override void Initialize()
        {
            base.Initialize();

            Visible = true;
        }

        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            var elapsedTime = gameTime.Elapsed.TotalSeconds;

            foreach (var sprite in playingSprites)
            {
                if (sprite.IsPaused)
                    continue;

                sprite.ElapsedTime += elapsedTime;

                // As long as we have some animations to play for the given sprite...
                while (sprite.Animations.Count > 0)
                {
                    var animationInfo = sprite.Animations.Peek();
                    var oneFrameTime = 1 / animationInfo.FramePerSeconds;

                    // As long as needed and possible go to the next animation frame
                    while (sprite.ElapsedTime >= oneFrameTime && (animationInfo.ShouldLoop || sprite.CurrentIndexIndex < animationInfo.SpriteIndices.Count - 1))
                    {
                        sprite.ElapsedTime -= oneFrameTime;
                        sprite.CurrentIndexIndex = (sprite.CurrentIndexIndex + 1) % animationInfo.SpriteIndices.Count;
                    }

                    // set the sprite frame
                    var provider = sprite.SpriteProvider as IAnimatableSpriteProvider;
                    if (provider != null)
                        provider.CurrentFrame = animationInfo.SpriteIndices[sprite.CurrentIndexIndex];

                    // we reached the end of the animation -> go to next animation
                    if (sprite.ElapsedTime >= oneFrameTime)
                    {
                        sprite.ElapsedTime -= oneFrameTime; // consider that one frame elapse between the two animations
                        sprite.RecycleFirstAnimation();
                    }
                    else // animation is not finished yet -> exit loop
                    {
                        break;
                    }
                }

                // There is no more animations to play for this sprite -> remove it from the sprite to animate list
                if (sprite.Animations.Count == 0)
                    spritesToStop.Add(sprite);
            }

            // actually stops the sprites that have finished their animation
            foreach (var spriteComponent in spritesToStop)
            {
                playingSprites.Remove(spriteComponent);
                spriteComponent.CurrentIndexIndex = 0;
                spriteComponent.ElapsedTime = 0;
            }
            spritesToStop.Clear();
        }

        /// <summary>
        /// Play the sprite animation starting at index <paramref name="startIndex"/> and ending at <paramref name="endIndex"/>.
        /// </summary>
        /// <param name="spriteComponent">The sprite component containing the animation</param>
        /// <param name="startIndex">The first index of the animation</param>
        /// <param name="endIndex">The last index of the animation</param>
        /// <param name="repeatMode">The value indicating how to loop the animation</param>
        /// <param name="framesPerSeconds">The animation speed in frames per second. 0 to use the sprite animation system default speed.</param>
        /// <param name="clearQueuedAnimations">Indicate if queued animation should be cleared</param>
        public void Play(SpriteComponent spriteComponent, int startIndex, int endIndex, AnimationRepeatMode repeatMode, float framesPerSeconds = 0, bool clearQueuedAnimations = true)
        {
            if (spriteComponent == null)
                return;

            var animationInfo = new SpriteComponent.AnimationInfo
            {
                ShouldLoop = repeatMode == AnimationRepeatMode.LoopInfinite,
                SpriteIndices = SpriteComponent.GetNewSpriteIndicesList(),
                FramePerSeconds = framesPerSeconds > 0 ? framesPerSeconds : DefaultFramesPerSecond,
            };

            for (int i = startIndex; i <= endIndex; i++)
                animationInfo.SpriteIndices.Add(i);

            spriteComponent.RecycleFirstAnimation();
            spriteComponent.Animations.Enqueue(animationInfo);
            var queuedAnimationsCount = spriteComponent.Animations.Count - 1;
            for (int i = 0; i < queuedAnimationsCount; i++)
            {
                var queuedAnimation = spriteComponent.Animations.Dequeue();
                if (!clearQueuedAnimations)
                    spriteComponent.Animations.Enqueue(queuedAnimation);
            }

            playingSprites.Add(spriteComponent);
            spriteComponent.ElapsedTime = 0;
            spriteComponent.CurrentIndexIndex = 0;
            spriteComponent.IsPaused = false;
        }

        /// <summary>
        /// Play the sprite animation defined by the provided sequence of indices.
        /// </summary>
        /// <param name="spriteComponent">The sprite component containing the animation</param>
        /// <param name="indices">The sequence of indices defining the sprite animation</param>
        /// <param name="repeatMode">The value indicating how to loop the animation</param>
        /// <param name="framesPerSeconds">The animation speed in frames per second. 0 to use the sprite animation system default speed.</param>
        /// <param name="clearQueuedAnimations">Indicate if queued animation should be cleared</param>
        public void Play(SpriteComponent spriteComponent, int[] indices, AnimationRepeatMode repeatMode, float framesPerSeconds = 0, bool clearQueuedAnimations = true)
        {
            if (spriteComponent == null)
                return;

            var animationInfo = new SpriteComponent.AnimationInfo
            {
                ShouldLoop = repeatMode == AnimationRepeatMode.LoopInfinite,
                SpriteIndices = SpriteComponent.GetNewSpriteIndicesList(),
                FramePerSeconds = framesPerSeconds > 0 ? framesPerSeconds : DefaultFramesPerSecond,
            };

            foreach (var i in indices)
                animationInfo.SpriteIndices.Add(i);

            spriteComponent.RecycleFirstAnimation();
            spriteComponent.Animations.Enqueue(animationInfo);
            var queuedAnimationsCount = spriteComponent.Animations.Count - 1;
            for (int i = 0; i < queuedAnimationsCount; i++)
            {
                var queuedAnimation = spriteComponent.Animations.Dequeue();
                if (!clearQueuedAnimations)
                    spriteComponent.Animations.Enqueue(queuedAnimation);
            }

            playingSprites.Add(spriteComponent);
            spriteComponent.ElapsedTime = 0;
            spriteComponent.CurrentIndexIndex = 0;
            spriteComponent.IsPaused = false;
        }

        /// <summary>
        /// Queue the sprite animation starting at index <paramref name="startIndex"/> and ending at <paramref name="endIndex"/> at the end of the animation queue.
        /// </summary>
        /// <param name="spriteComponent">The sprite component containing the animation</param>
        /// <param name="startIndex">The first index of the animation</param>
        /// <param name="endIndex">The last index of the animation</param>
        /// <param name="repeatMode">The value indicating how to loop the animation</param>
        /// <param name="framesPerSeconds">The animation speed in frames per second. 0 to use the sprite animation system default speed.</param>
        public void Queue(SpriteComponent spriteComponent, int startIndex, int endIndex, AnimationRepeatMode repeatMode, float framesPerSeconds = 0)
        {
            if (spriteComponent == null)
                return;

            var animationInfo = new SpriteComponent.AnimationInfo
            {
                ShouldLoop = repeatMode == AnimationRepeatMode.LoopInfinite,
                FramePerSeconds = framesPerSeconds > 0 ? framesPerSeconds : DefaultFramesPerSecond,
                SpriteIndices = SpriteComponent.GetNewSpriteIndicesList(),
            };

            for (int i = startIndex; i <= endIndex; i++)
                animationInfo.SpriteIndices.Add(i);

            spriteComponent.Animations.Enqueue(animationInfo);

            playingSprites.Add(spriteComponent);
        }

        /// <summary>
        /// Queue the sprite animation defined by the provided sequence of indices at the end of the animation queue.
        /// </summary>
        /// <param name="spriteComponent">The sprite component containing the animation</param>
        /// <param name="indices">The sequence of indices defining the sprite animation</param>
        /// <param name="repeatMode">The value indicating how to loop the animation</param>
        /// <param name="framesPerSeconds">The animation speed in frames per second. 0 to use the sprite animation system default speed.</param>
        public void Queue(SpriteComponent spriteComponent, int[] indices, AnimationRepeatMode repeatMode, float framesPerSeconds = 0)
        {
            if (spriteComponent == null)
                return;

            var animationInfo = new SpriteComponent.AnimationInfo
            {
                ShouldLoop = repeatMode == AnimationRepeatMode.LoopInfinite,
                FramePerSeconds = framesPerSeconds > 0 ? framesPerSeconds : DefaultFramesPerSecond,
                SpriteIndices = SpriteComponent.GetNewSpriteIndicesList(),
            };

            foreach (var i in indices)
                animationInfo.SpriteIndices.Add(i);

            spriteComponent.Animations.Enqueue(animationInfo);

            playingSprites.Add(spriteComponent);
        }

        /// <summary>
        /// Pauses the animation of the provided sprite component.
        /// </summary>
        /// <param name="spriteComponent">the sprite component to pause</param>
        public void Pause(SpriteComponent spriteComponent)
        {
            spriteComponent.IsPaused = true;
        }

        /// <summary>
        /// Resumes a previously paused animation.
        /// </summary>
        /// <param name="spriteComponent">the sprite component to resume</param>
        public void Resume(SpriteComponent spriteComponent)
        {
            spriteComponent.IsPaused = false;
        }

        /// <summary>
        /// Stops the animation of the provided sprite component.
        /// </summary>
        /// <param name="spriteComponent">the sprite component to stop</param>
        public void Stop(SpriteComponent spriteComponent)
        {
            spriteComponent.ElapsedTime = 0;
            spriteComponent.CurrentIndexIndex = 0;
            spriteComponent.ClearAnimations();
            playingSprites.Remove(spriteComponent);
        }
    }
}
