// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Animations;
using Stride.Engine;
using Stride.Input;
using Stride.Physics;
using Stride.Rendering.Sprites;

namespace PhysicsSample
{
    /// <summary>
    /// This script will interface the Physics character controller of the entity to move the character around
    /// Will also animate the sprite of the entity, between run and idle.
    /// </summary>
    public class CharacterScript : SyncScript
    {
        [Flags]
        enum PlayerState
        {
            Idle = 0x0,
            Run = 0x1,
            Jump = 0x2
        }

        private const float speed = 0.075f;

        private CharacterComponent playerController;
        private SpriteComponent playerSprite;

        [DataMember(Mask = LiveScriptingMask)] // keep the value when reloading the script (live-scripting)
        private bool movingToTarget;

        [DataMember(Mask = LiveScriptingMask)] // keep the value when reloading the script (live-scripting)
        private PlayerState oldState = PlayerState.Idle;

        [DataMember(Mask = LiveScriptingMask)] // keep the value when reloading the script (live-scripting)
        private Vector3 oldDirection = Vector3.Zero;

        [DataMember(Mask = LiveScriptingMask)] // keep the value when reloading the script (live-scripting)
        private Vector3 autoPilotTarget = Vector3.Zero;

        void PlayIdle()
        {
            var sheet = ((SpriteFromSheet)playerSprite.SpriteProvider).Sheet;
            SpriteAnimation.Play(playerSprite, sheet.FindImageIndex("idle0"), sheet.FindImageIndex("idle4"), AnimationRepeatMode.LoopInfinite, 7);
        }

        void PlayRun()
        {
            var sheet = ((SpriteFromSheet)playerSprite.SpriteProvider).Sheet;
            SpriteAnimation.Play(playerSprite, sheet.FindImageIndex("run0"), sheet.FindImageIndex("run4"), AnimationRepeatMode.LoopInfinite, 12);
        }

        public override void Start()
        {
            playerSprite = Entity.Get<SpriteComponent>();
            playerController = Entity.Get<CharacterComponent>();

            //Please remember that in the GameStudio element the parameter Step Height is extremely important, it not set properly it will cause the entity to snap fast to the ground
            playerController.JumpSpeed = 5.0f;
            playerController.Gravity = new Vector3(0.0f, -10.0f, 0.0f);
            playerController.FallSpeed = 10.0f;
            playerController.ProcessCollisions = true;

            if (!IsLiveReloading)
            {
                Script.AddTask(async () =>
                {
                    while (Game.IsRunning)
                    {
                        var collision = await playerController.NewCollision();
                        // Stop if we collide from side
                        foreach (var contact in collision.Contacts)
                        {
                            if (contact.Normal.X < -0.5f || contact.Normal.X > 0.5f)
                            {
                                movingToTarget = false;
                                break;
                            }
                        }
                    }
                });
                PlayIdle();
            }
        }

        public override void Update()
        {
            var playerState = PlayerState.Idle;
            var playerDirection = Vector3.Zero;

            // -- Keyboard Inputs

            // Space bar = jump
            if (Input.IsKeyDown(Keys.Space))
            {
                playerState |= PlayerState.Jump;
            }

            // Left - right = run
            if (Input.IsKeyDown(Keys.Right))
            {
                movingToTarget = false;
                playerState |= PlayerState.Run;
                playerDirection = Vector3.UnitX * speed;
            }
            else if (Input.IsKeyDown(Keys.Left))
            {
                movingToTarget = false;
                playerState |= PlayerState.Run;
                playerDirection = -Vector3.UnitX * speed;
            }

            // -- Pointer (mouse/touch)
            foreach (var pointerEvent in Input.PointerEvents.Where(pointerEvent => pointerEvent.EventType == PointerEventType.Pressed))
            {
                if (!movingToTarget)
                {
                    var screenX = (pointerEvent.Position.X - 0.5f) * 2.0f;
                    screenX *= 8.75f;

                    autoPilotTarget = new Vector3(screenX, 0, 0);

                    movingToTarget = true;
                }
                else
                {
                    playerState |= PlayerState.Jump;
                }
            }

            // -- Logic

            // are we autopiloting?
            if (movingToTarget)
            {
                var direction = autoPilotTarget - Entity.Transform.Position;
                direction.Y = 0;

                //should we stop?
                var length = direction.Length();
                if (length < speed)
                {
                    movingToTarget = false;

                    playerDirection = Vector3.Zero;

                    playerState = PlayerState.Idle;
                }
                else
                {
                    direction.Normalize();

                    playerDirection = (direction.X > 0 ? Vector3.UnitX : -Vector3.UnitX) * speed;

                    playerState |= PlayerState.Run;
                }
            }

            // did we start jumping?
            if (playerState.HasFlag(PlayerState.Jump) && !oldState.HasFlag(PlayerState.Jump))
            {
                playerController.Jump();
            }

            // did we just land?
            if (oldState.HasFlag(PlayerState.Jump))
            {
                if (!playerController.IsGrounded)
                {
                    //force set jump flag
                    if (!playerState.HasFlag(PlayerState.Jump))
                    {
                        playerState |= PlayerState.Jump;
                        // Mantain motion 
                        playerDirection = oldDirection;
                    }
                }
                else if (playerController.IsGrounded)
                {
                    //force clear jump flag
                    if (playerState.HasFlag(PlayerState.Jump))
                    {
                        playerState ^= PlayerState.Jump;
                    }
                }
            }

            // did we start running?
            if (playerState.HasFlag(PlayerState.Run) && !oldState.HasFlag(PlayerState.Run))
            {
                PlayRun();
            }
            // did we stop running?
            else if (!playerState.HasFlag(PlayerState.Run) && oldState.HasFlag(PlayerState.Run))
            {
                PlayIdle();
            }

            // movement logic
            if (oldDirection != playerDirection)
            {
                playerController.Move(playerDirection);

                if (playerState.HasFlag(PlayerState.Run))
                {
                    if ((playerDirection.X > 0 && Entity.Transform.Scale.X < 0) ||
                        (playerDirection.X < 0 && Entity.Transform.Scale.X > 0))
                    {
                        Entity.Transform.Scale.X *= -1.0f;
                    }
                }
            }

            // Store current state for next frame
            oldState = playerState;
            oldDirection = playerDirection;
        }
    }
}
