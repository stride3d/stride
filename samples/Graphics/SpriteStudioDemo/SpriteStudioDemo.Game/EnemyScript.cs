// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;
using System;
using System.Threading.Tasks;
using Stride.Animations;
using System.Diagnostics;

namespace SpriteStudioDemo
{
    public class EnemyScript : AsyncScript
    {
        private const float enemyInitPositionY = 20;

        // enemy position
        private const float gameWidthX = 16f; // from -8f to 8f
        private const float gameWidthHalfX = gameWidthX / 2f;

        private const float enemyLifeBase = 3.0f;
        private float enemyLife = 3.0f;

        private RigidbodyComponent rigidbodyElement;
        private AnimationComponent animationComponent;

        PlayingAnimation playingAnimation;

        // random
        private static readonly int seed = Environment.TickCount;
        private static readonly Random enemyRandomLocal = new Random(seed);

        private async Task Reset()
        {
            rigidbodyElement.IsKinematic = true; //sto motion and set kinematic (listen to our transform changes)
            rigidbodyElement.IsTrigger = true; //set as ghost (bullets will go thru)

            Entity.Transform.Position.Y = enemyInitPositionY;

            var random = enemyRandomLocal;
            // Appearance position
            Entity.Transform.Position.X = (((float)(random.NextDouble())) * gameWidthX) - gameWidthHalfX;
            // Waiting time
            enemyLife = enemyLifeBase + (enemyLifeBase * (float)random.NextDouble());

            Entity.Transform.UpdateWorldMatrix();
            rigidbodyElement.UpdatePhysicsTransformation();

            if (playingAnimation == null || playingAnimation.Name != "Wait")
            {
                playingAnimation = animationComponent.Play("Wait");
            }

            await Script.NextFrame();

            rigidbodyElement.IsKinematic = false;
            rigidbodyElement.IsTrigger = false;
            rigidbodyElement.Activate();
        }

        Task exploding;

        public void Explode()
        {
            rigidbodyElement.IsKinematic = true;
            rigidbodyElement.IsTrigger = true;

            if (playingAnimation == null || playingAnimation.Name != "Dead")
            {
                playingAnimation = animationComponent.Play("Dead");
            }

            exploding = WaitMs(1500);
        }

        readonly Stopwatch _watch = Stopwatch.StartNew();

        public async Task WaitMs(int ms)
        {
            var start = _watch.ElapsedMilliseconds;
            while (_watch.ElapsedMilliseconds < start + ms)
            {
                await Script.NextFrame();
            }
        }

        public override async Task Execute()
        {
            animationComponent = Entity.Get<AnimationComponent>();

            rigidbodyElement = Entity.Get<RigidbodyComponent>();
            rigidbodyElement.LinearFactor = new Vector3(0, 1, 0); //allow only Y motion
            rigidbodyElement.AngularFactor = new Vector3(0, 0, 0); //allow no rotation

            await Reset();

            while (Game.IsRunning)
            {
                await WaitMs((int)(enemyLife * 1000));

                if (exploding != null)
                {
                    await exploding;
                    exploding = null;
                }

                await Reset();
            }
        }
    }
}
