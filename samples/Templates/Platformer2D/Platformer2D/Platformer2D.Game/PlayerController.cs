using System;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Engine;
using Stride.Core;
using Stride.Physics;
using System.Windows.Input;
using Stride.Graphics;
using Stride.Rendering.Sprites;
using System.Collections.Generic;

namespace Platformer2D.Player;

public class PlayerController : SyncScript
{
    public required CharacterComponent CharacterComponent { get; init; }
    
    public required SpriteComponent Sprite { get; init; }
    
    private float animationTimer = 0f;
    // Animation Spped: Every 1/10 passed seconds the next frame will be played. Closer to 1.0 is a slower Animation.
    private const float animationInterval = 1f / 5f;
    private int idleFrame = 0;
    private int runFrame = 0;
    private int jumpFrame = 0;

    private const int IDLE_FRAME_END = 4;
    private const int RUN_FRAME_END = 16;
    private const int JUMP_FRAME_END = 21;
    private const int JUMP_FRAME_COUNT = 6;
    
    // units per second (is multiplied with Vector.UnitX which is 1)
    private const float MOVE_SPEED = 5f;
    
    private bool isFacingRight = true;

    private SpriteFromSheet SpriteSheet => (SpriteFromSheet)Sprite.SpriteProvider;

    public override void Update()
    {
        // HandleAnimation uses bool isMoving from the users Input
        HandleAnimation(HandleInput());
    }
    
    /// <summary>
    /// Handles both the Input of the Player via Keyboard (WAD- & Arrow-Keys) and the resulting Animation at the end according to input
    /// </summary>
    private bool HandleInput()
    {
        bool isMoving = false;
        if (Input.HasKeyboard)
        {
            var moveDirection = Vector3.Zero;
            
            CharacterComponent.SetVelocity(Vector3.Zero);

            // Left
            if (Input.IsKeyDown(Keys.A) || Input.IsKeyDown(Keys.Left))
            {
                isMoving = true;
                if (isFacingRight)
                {
                    isFacingRight = false;
                    CharacterComponent.Orientation = Quaternion.RotationY(MathUtil.DegreesToRadians(180));
                }
                moveDirection  = -Vector3.UnitX * MOVE_SPEED;
                
            }
            
            // Right
            if (Input.IsKeyDown(Keys.D) || Input.IsKeyDown(Keys.Right))
            {
                isMoving = true;
                
                if (!isFacingRight)
                {
                    isFacingRight = true;
                    CharacterComponent.Orientation = Quaternion.RotationY(MathUtil.DegreesToRadians(0));
                }
                moveDirection = Vector3.UnitX * MOVE_SPEED;
            }
            
            // Up (Jump)
            if ((Input.IsKeyPressed(Keys.W) || Input.IsKeyPressed(Keys.Up)) && CharacterComponent.IsGrounded)
            {
                CharacterComponent.Jump();
            }
            CharacterComponent.SetVelocity(moveDirection);
            HandleAnimation(isMoving);
        }
        return isMoving;
    }
    
    private void HandleAnimation(bool isMoving)
    {
        animationTimer += (float) Game.UpdateTime.Elapsed.TotalSeconds;
        
        if (CharacterComponent.IsGrounded && !isMoving)
        {
            PlayIdleAnimation();
        }
            
        if (CharacterComponent.IsGrounded && isMoving)
        {
            PlayRunAnimation();
        }
        
        if (!CharacterComponent.IsGrounded)
        {
            PlayJumpAnimation();
        }
    }
    
    /// <summary>
    /// When there is no input, play idle-Animation. Which loops frames 0-3.
    /// </summary>
    private void PlayIdleAnimation()
    {
        if (animationTimer >= animationInterval)
        {
            animationTimer -= animationInterval;
            SpriteSheet.CurrentFrame = idleFrame;
            idleFrame = (idleFrame + 1) % IDLE_FRAME_END;
        }
        ResetFrameCounts(ref runFrame, ref jumpFrame);
    }
    
    /// <summary>
    /// When there is movement to either side, play run-animation. Which loops frames 4-19
    /// </summary>
    private void PlayRunAnimation()
    {
        if (animationTimer >= animationInterval)
        {
            animationTimer -= animationInterval;
            SpriteSheet.CurrentFrame = IDLE_FRAME_END + runFrame;
            runFrame = (runFrame + 1) % RUN_FRAME_END;
        }
        ResetFrameCounts(ref idleFrame, ref jumpFrame);
    }
    
    /// <summary>
    /// When the character is not grounded, play jump-animation. Which only plays frames 20-27 once and then resets when the character is grounded.
    /// </summary>
    private void PlayJumpAnimation()
    {
        if (animationTimer >= animationInterval)
        {
            animationTimer -= animationInterval;
            SpriteSheet.CurrentFrame = JUMP_FRAME_END + jumpFrame;
            
            if (jumpFrame < JUMP_FRAME_COUNT)
            {
                jumpFrame++;
            }
        }
        ResetFrameCounts(ref idleFrame, ref runFrame);
    }
    
    /// <summary>
    /// Resets frame counters to 0.
    /// Example: When playing the run animation, this is called to reset the idle and jump animation counter, to ensure they start at their first frame.
    /// </summary>
    private void ResetFrameCounts(ref int counter, ref int counter2)
    {
        counter = 0;
        counter2 = 0;
    }
}
