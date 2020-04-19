// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Animations;
using Stride.Engine;
using Stride.Graphics;
using Stride.Input;
using Stride.Physics;
using Stride.Rendering.Sprites;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpriteStudioDemo
{
    public class PlayerScript : AsyncScript
    {
        private enum AgentAnimation
        {
            Run,
            Idle,
            Shoot
        }

        // InputState represents all command inputs from a user
        private enum InputState
        {
            None,
            RunLeft,
            RunRight,
            Shoot,
        }

        // TODO centralize
        private const float gameWidthX = 16f;       // from -8f to 8f

        private const float gameWidthHalfX = gameWidthX / 2f;

        private const int AgentMoveDistance = 10;       // virtual resolution unit/second
        private const float AgentShootDelay = 0.3f;     // second

        private static readonly Dictionary<AgentAnimation, int> AnimationFps = new Dictionary<AgentAnimation, int> { { AgentAnimation.Run, 12 }, { AgentAnimation.Idle, 7 }, { AgentAnimation.Shoot, 15 } };

        private SpriteComponent agentSpriteComponent;
        private SpriteSheet spriteSheet;

        // Touch input state
        private PointerEvent pointerState;

        private bool isPointerDown; // Cache state if a user is current touching the screen.

        [DataMember(Mask = LiveScriptingMask)] // keep the value when reloading the script (live-scripting)
        private bool isAgentFacingRight;

        [DataMember(Mask = LiveScriptingMask)] // keep the value when reloading the script (live-scripting)
        private float shootDelayCounter;

        [DataMember(Mask = LiveScriptingMask)] // keep the value when reloading the script (live-scripting)
        private AgentAnimation currentAgentAnimation;

        private AgentAnimation CurrentAgentAnimation { get; set; }
		
		public SpriteSheet BulletSheet { get; set; }
		
		public PhysicsColliderShape BulletColliderShape { get; set; }

        public override async Task Execute()
        {
            spriteSheet = BulletSheet;
            agentSpriteComponent = Entity.Get<SpriteComponent>();
            var animComponent = Entity.Get<AnimationComponent>();
            PlayingAnimation playingAnimation = null;

            // Calculate offset of the bullet from the Agent if he is facing left and right side // TODO improve this
            var bulletOffset = new Vector3(1.3f, 1.65f, 0f);

            // Initialize game entities
            if (!IsLiveReloading)
            {
                shootDelayCounter = 0f;
                isAgentFacingRight = true;
                currentAgentAnimation = AgentAnimation.Idle;
            }
            CurrentAgentAnimation = currentAgentAnimation;

            var normalScaleX = Entity.Transform.Scale.X;

            var bulletCS = BulletColliderShape;

            Task animTask = null;

            while (Game.IsRunning)
            {
                await Script.NextFrame();

                var inputState = GetKeyboardInputState();

                if (inputState == InputState.None)
                    inputState = GetPointerInputState();

                if (inputState == InputState.RunLeft || inputState == InputState.RunRight)
                {
                    // Update Agent's position
                    var dt = (float)Game.UpdateTime.Elapsed.TotalSeconds;

                    Entity.Transform.Position.X += ((inputState == InputState.RunRight) ? AgentMoveDistance : -AgentMoveDistance) * dt;

                    if (Entity.Transform.Position.X < -gameWidthHalfX)
                        Entity.Transform.Position.X = -gameWidthHalfX;

                    if (Entity.Transform.Position.X > gameWidthHalfX)
                        Entity.Transform.Position.X = gameWidthHalfX;

                    isAgentFacingRight = inputState == InputState.RunRight;

                    // If agent face left, flip the sprite
                    Entity.Transform.Scale.X = isAgentFacingRight ? normalScaleX : -normalScaleX;

                    // Update the sprite animation and state
                    CurrentAgentAnimation = AgentAnimation.Run;
                    if (playingAnimation == null || playingAnimation.Name != "Run")
                    {
                        playingAnimation = animComponent.Play("Run");
                    }
                }
                else if (inputState == InputState.Shoot)
                {
                    if(animTask != null && !animTask.IsCompleted) continue;
                    if (animTask != null && animTask.IsCompleted) playingAnimation = null;

                    animTask = null;

                    var rb = new RigidbodyComponent { CanCollideWith = CollisionFilterGroupFlags.CustomFilter1, CollisionGroup = CollisionFilterGroups.DefaultFilter };
                    rb.ColliderShapes.Add(new ColliderShapeAssetDesc { Shape = bulletCS });

                    // Spawns a new bullet
                    var bullet = new Entity
                    {
                        new SpriteComponent { SpriteProvider = SpriteFromSheet.Create(spriteSheet, "bullet") },
                        rb,
                        new BeamScript()
                    };
                    bullet.Name = "bullet";

                    bullet.Transform.Position = (isAgentFacingRight) ? Entity.Transform.Position + bulletOffset : Entity.Transform.Position + (bulletOffset * new Vector3(-1, 1, 1));
                    bullet.Transform.UpdateWorldMatrix();

                    SceneSystem.SceneInstance.RootScene.Entities.Add(bullet);

                    rb.LinearFactor = new Vector3(1, 0, 0);
                    rb.AngularFactor = new Vector3(0, 0, 0);
                    rb.ApplyImpulse(isAgentFacingRight ? new Vector3(25, 0, 0) : new Vector3(-25, 0, 0));

                    // Start animation for shooting
                    CurrentAgentAnimation = AgentAnimation.Shoot;
                    if (playingAnimation == null || playingAnimation.Name != "Attack")
                    {
                        playingAnimation = animComponent.Play("Attack");
                        animTask = animComponent.Ended(playingAnimation);
                    }
                }
                else
                {
                    CurrentAgentAnimation = AgentAnimation.Idle;
                    if (playingAnimation == null || playingAnimation.Name != "Stance")
                    {
                        playingAnimation = animComponent.Play("Stance");
                    }
                }
            }
        }

        /// <summary>
        /// Determine input from a user from a keyboard.
        /// Left and Right arrow for running to left and right direction, Space for shooting.
        /// </summary>
        /// <returns></returns>
        private InputState GetKeyboardInputState()
        {
            if (Input.IsKeyDown(Keys.Right))
                return InputState.RunRight;
            if (Input.IsKeyDown(Keys.Left))
                return InputState.RunLeft;

            return Input.IsKeyDown(Keys.Space) ? InputState.Shoot : InputState.None;
        }

        /// <summary>
        /// Determine input from a user from Pointer (Touch/Mouse).
        /// It analyses the input from a user, and transform it to InputState using in the game, which is then returned.
        /// </summary>
        /// <returns></returns>
        private InputState GetPointerInputState()
        {
            // Get new state of Pointer (Touch input)
            if (Input.PointerEvents.Any())
            {
                var lastPointer = Input.PointerEvents.Last();
                if (lastPointer.EventType == PointerEventType.Pressed)
                    isPointerDown = true;
                else if (lastPointer.EventType == PointerEventType.Released)
                    isPointerDown = false;
                pointerState = lastPointer;
            }

            // If a user does not touch the screen, there is not input
            if (!isPointerDown)
			{
                return InputState.None;
			}
			
            // Transform pointer's position from normorlize coordinate to virtual resolution coordinate
            var resolution = new Vector2(GraphicsDevice.Presenter.BackBuffer.Width, GraphicsDevice.Presenter.BackBuffer.Height);
            var virtualCoordinatePointerPositionA = resolution.X * (pointerState.Position.X + 0.05f);
            var virtualCoordinatePointerPositionB = resolution.X * (pointerState.Position.X - 0.05f);

            var virtualX = VirtualCoordToPixel(Entity.Transform.Position.X);

            // Check if the touch position is in the x-axis region of the agent's sprite; if so, input is shoot
            if (virtualX <= virtualCoordinatePointerPositionA && virtualCoordinatePointerPositionB <= virtualX)
			{
                return InputState.Shoot;
			}
			
            // Check if a pointer falls left or right of the screen, which would correspond to Run to the left or right respectively
            return ((pointerState.Position.X) <= virtualX / resolution.X) ? InputState.RunLeft : InputState.RunRight;
        }

        private float VirtualCoordToPixel(float virtualCoord)
        {
            return (virtualCoord + (gameWidthHalfX)) / gameWidthX * GraphicsDevice.Presenter.BackBuffer.Width;
        }
    }
}
