// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace JumpyJet
{
    /// <summary>
    /// PipeSet contains Two pipes: Top and Bottom pipes.
    /// </summary>
    public class PipeSet
    {
        public Entity Entity = new Entity("Pipe root entity");

        private const float VerticalDistanceBetweenPipe = 230f;

        private readonly Entity topPipe;
        private readonly Entity bottomPipe;

        private readonly float scrollSpeed;
        private readonly float pipeWidth;
        private readonly float pipeHeight;
        private readonly float halfPipeWidth;

        private readonly Random random = new Random();
        private readonly float startScrollPos;
        private readonly float halfScrollWidth;

        private readonly RectangleF pipeCollider;

        public PipeSet(Entity referencePipeEntity, float scrollSpeed, float startScrollPos, float screenWidth)
        {
            this.scrollSpeed = scrollSpeed;
            this.startScrollPos = startScrollPos;
            halfScrollWidth = screenWidth / 2f;

            // Store Entity and create another one for two rendering:
            // top and bottom sprite of pipe.
            var spriteComp = referencePipeEntity.Get<SpriteComponent>();
             bottomPipe = referencePipeEntity.Clone();
            topPipe = referencePipeEntity.Clone();
            Entity.AddChild(bottomPipe);
            Entity.AddChild(topPipe);

            var sprite = spriteComp.CurrentSprite;
            pipeWidth = sprite.SizeInPixels.X;
            pipeHeight = sprite.SizeInPixels.Y;
            halfPipeWidth = pipeWidth/2f;

            // Setup pipeCollider
            pipeCollider = new RectangleF(0, 0, pipeWidth, pipeHeight);

            // Place the top/bottom pipes relatively to the root.
            topPipe.Transform.Position.Y = -(VerticalDistanceBetweenPipe + pipeHeight) * 0.5f;
            bottomPipe.Transform.Position.Y = (VerticalDistanceBetweenPipe + pipeHeight) * 0.5f;
            bottomPipe.Transform.Rotation = Quaternion.RotationZ(MathUtil.Pi);

            ResetPipe();
        }

        public void ResetPipe()
        {
            ResetPipe(startScrollPos);
        }

        public void ResetPipe(float resetScrollPos)
        {
            // Set a random height to the pipe set.
            Entity.Transform.Position = new Vector3(resetScrollPos, random.Next(-50, 225), 0);
        }

        public void Update(float elapsedTime)
        {
            // Update pos according to the speed
            Entity.Transform.Position.X += (int)(elapsedTime * scrollSpeed);
        }

        public RectangleF GetTopPipeCollider()
        {
            return GetCollider(topPipe);
        }

        public RectangleF GetBottomPipeCollider()
        {
            return GetCollider(bottomPipe);
        }

        private RectangleF GetCollider(Entity entity)
        {
            entity.Transform.UpdateWorldMatrix();
            var position = entity.Transform.WorldMatrix.TranslationVector;

            var collider = pipeCollider;
            collider.X = position.X - collider.Width/2;
            collider.Y = position.Y - collider.Height/2;

            return collider;
        }

        /// <summary>
        /// Returns a value indicating the pipe has been passed or not
        /// </summary>
        /// <param name="positionX">The position along the X axis</param>
        /// <returns><value>true</value> if the pipe set has been passed, <value>false</value> otherwise</returns>
        public bool HasBeenPassed(float positionX)
        {
            return Entity.Transform.Position.X + halfPipeWidth < positionX;
        }

        /// <summary>
        /// Determine if the content is visible in the screen.
        /// The content is invisible when the right side of the quad
        ///  passes the most left side of the screen.
        /// </summary>
        /// <returns></returns>
        public bool IsOutOfScreenLeft()
        {
            return Entity.Transform.Position.X + halfPipeWidth < -halfScrollWidth;
        }
    }
}
