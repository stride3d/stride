// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Events;
using Stride.Input;
using Stride.Physics;
using Stride.Rendering.Sprites;

namespace JumpyJet
{
    /// <summary>
    /// CharacterScript is controlled by a user.
    /// The control is as follow, tapping a screen/clicking a mouse will make the agent jump up.
    /// </summary>
    public class CharacterScript : AsyncScript
    {
        private EventReceiver gameResetListener = new EventReceiver(GameGlobals.GameResetEventKey);
        private EventReceiver gameStartedListener = new EventReceiver(GameGlobals.GameStartedEventKey);

        private static readonly Vector3 Gravity = new Vector3(0, -17, 0);
        private static readonly Vector3 StartPos = new Vector3(-1, 0, 0);
        private static readonly Vector3 StartVelocity = new Vector3(0, 7, 0);

        private const float TopLimit = (568 - 200) * GameGlobals.GamePixelToUnitScale;
        private const float NormalVelocityY = 6.5f;
        private const float VelocityAboveTopLimit = 2f;
        private const int FlyingSpriteFrameIndex = 1;
        private const int FallingSpriteFrameIndex = 0;

        private Vector3 position;
        private Vector3 rotation;

        private bool isRunning;
        private Vector3 velocity;

        public void Start()
        {
            position = StartPos;
            velocity = StartVelocity;

            Reset();

            Script.AddTask(CountPassedPipes);
            Script.AddTask(DetectGameOver);
        }

        /// <summary>
        /// Reset CharacterScript parameters: position, velocity and set state.
        /// </summary>
        public void Reset()
        {
            position.Y = 0;
            rotation.Z = 0f;
            UpdateTransformation();

            velocity = StartVelocity;
            isRunning = false;

            var provider = Entity.Get<SpriteComponent>().SpriteProvider as SpriteFromSheet;
            if (provider != null)
                provider.CurrentFrame = FallingSpriteFrameIndex;
        }

        /// <summary>
        /// Update the agent according to its states: {Idle, Alive, Die}
        /// </summary>
        public async Task CountPassedPipes()
        {
            var physicsComponent = Entity.Components.Get<PhysicsComponent>();

            while (Game.IsRunning)
            {
                var collision = await physicsComponent.NewCollision();

                if (collision.ColliderA.CollisionGroup == CollisionFilterGroups.CustomFilter1 || // use collision group 1 to distinguish pipe passed trigger from other colliders.
                    collision.ColliderB.CollisionGroup == CollisionFilterGroups.CustomFilter1)
                    GameGlobals.PipePassedEventKey.Broadcast();
            }
        }

        /// <summary>
        /// Update the agent according to its states: {Idle, Alive, Die}
        /// </summary>
        public async Task DetectGameOver()
        {
            var physicsComponent = Entity.Components.Get<PhysicsComponent>();

            while (Game.IsRunning)
            {
                await Script.NextFrame();

                // detect collisions with the pipes
                var collision = await physicsComponent.NewCollision();
                if (collision.ColliderA.CollisionGroup == CollisionFilterGroups.DefaultFilter &&
                    collision.ColliderB.CollisionGroup == CollisionFilterGroups.DefaultFilter)
                {
                    isRunning = false;
                    GameGlobals.GameOverEventKey.Broadcast();
                }
            }
        }

        /// <summary>
        /// Update the agent according to its states: {Idle, Alive, Die}
        /// </summary>
        public override async Task Execute()
        {
            Start();

            while (Game.IsRunning)
            {
                await Script.NextFrame();

                if (gameResetListener.TryReceive())
                    Reset();

                if (gameStartedListener.TryReceive())
                    isRunning = true;

                if (!isRunning)
                    continue;

                var elapsedTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;

                // apply impulse on the touch/space
                if (Input.IsKeyPressed(Keys.Space) || UserTappedScreen())
                    velocity.Y = position.Y > TopLimit ? VelocityAboveTopLimit : NormalVelocityY;

                // update position/velocity
                velocity += Gravity * elapsedTime;
                position += velocity * elapsedTime;

                // update animation and rotation value
                UpdateAgentAnimation();

                // update the position/rotation
                UpdateTransformation();
            }
        }

        private void UpdateTransformation()
        {
            Entity.Transform.Position = position;
            Entity.Transform.RotationEulerXYZ = rotation;
        }

        private bool UserTappedScreen()
        {
            return Input.PointerEvents.Any(pointerEvent => pointerEvent.EventType == PointerEventType.Pressed);
        }

        private void UpdateAgentAnimation()
        {
            var isFalling = velocity.Y < 0;
            var rotationSign = isFalling ? -1 : 1;

            // Set falling sprite frame
            var provider = Entity.Get<SpriteComponent>().SpriteProvider as SpriteFromSheet;
            if (provider != null)
                provider.CurrentFrame = isFalling ? FallingSpriteFrameIndex : FlyingSpriteFrameIndex;

            // Rotate a sprite
            rotation.Z += rotationSign * MathUtil.Pi * 0.01f;
            if (rotationSign * rotation.Z > Math.PI / 10f)
                rotation.Z = rotationSign * MathUtil.Pi / 10f;
        }
    }
}
