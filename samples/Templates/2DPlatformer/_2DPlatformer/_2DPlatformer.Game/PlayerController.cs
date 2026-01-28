using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Engine;
using Stride.Core;
using Stride.Physics;
using System.Windows.Input;
using Stride.Graphics;
using Stride.Rendering.Sprites;
using SharpFont.MultipleMasters;

namespace _2DPlatformer.Player;

public class PlayerController : SyncScript
{
    private CharacterComponent characterComponent;
    
    [Display("Character Sprite Sheet")]
    public SpriteSheet CoinSprites { get; set; }
    private SpriteFromSheet spriteComponent;
    
    private double animationTimer = 0f;
    private readonly float animationInterval = 1f / 10f;
    private int runFrame = 0;
    private int jumpFrame = 0;
    
    private readonly float moveSpeed = 5f;
    
    private bool isFacingRight = true;

    public override void Start()
    {
        characterComponent = Entity.Get<CharacterComponent>();
        spriteComponent = Entity.Get<SpriteComponent>().SpriteProvider as SpriteFromSheet;
    }

    public override void Update()
    {
         HandleInput();
    }
    
    /// <summary>
    /// Handles both the Input of the Player via Keyboard (WAS- & Arrow-Keys) and the resulting Animation at the end according to input
    /// </summary>
    private void HandleInput()
    {
         if (Input.HasKeyboard)
            {
                var moveDirection = Vector3.Zero;
                bool isMoving = false;
                
                characterComponent.SetVelocity(Vector3.Zero);

                // Left
                if (Input.IsKeyDown(Keys.A) || Input.IsKeyDown(Keys.Left))
                {
                    isMoving = true;
                    if (isFacingRight)
                    {
                        isFacingRight = false;
                        characterComponent.Orientation = Quaternion.RotationY(MathUtil.DegreesToRadians(180));
                    }
                    
                    moveDirection  = -Vector3.UnitX * moveSpeed;
                    
                }
                
                // Right
                if (Input.IsKeyDown(Keys.D) || Input.IsKeyDown(Keys.Right))
                {
                    isMoving = true;
                    
                    if (!isFacingRight)
                    {
                        isFacingRight = true;
                        characterComponent.Orientation = Quaternion.RotationY(MathUtil.DegreesToRadians(0));
                    }
                    moveDirection = Vector3.UnitX * moveSpeed;
                }
                
                // Up (Jump)
                if (Input.IsKeyDown(Keys.W) && characterComponent.IsGrounded || Input.IsKeyDown(Keys.Up) && characterComponent.IsGrounded)
                {
                    characterComponent.Jump();
                }
                
                characterComponent.SetVelocity(moveDirection);
                HandleAnimation(isMoving);
            }
    }
    
    
    private void HandleAnimation(bool isMoving)
    {
    
        animationTimer += Game.UpdateTime.Elapsed.TotalSeconds;
        
        if (characterComponent.IsGrounded && !isMoving)
        {
            PlayIdleAnimation();
        }
            
        if (characterComponent.IsGrounded && isMoving)
        {
            PlayRunAnimation();
        }
        
        if (!characterComponent.IsGrounded)
        {
            PlayJumpAnimation();
        }
    }
    
    /// <summary>
    /// When there is no input, play idle-Animation. Which loops frames 0-3.
    /// </summary>
    private void PlayIdleAnimation()
    {
        jumpFrame = 0;
        
        if (animationTimer >= animationInterval)
        {
            animationTimer = 0f;
            spriteComponent.CurrentFrame = (spriteComponent.CurrentFrame + 1) % 4;
        }
    }
    
    /// <summary>
    /// When there is movement to either side, play run-animation. Which loops frames 4-19
    /// </summary>
    private void PlayRunAnimation()
    {
        jumpFrame = 0;
        if (animationTimer >= animationInterval)
        {
            animationTimer = 0f;
            runFrame = (runFrame + 1) % 16;
            spriteComponent.CurrentFrame = 4 + runFrame;
        }
    }
    
    /// <summary>
    /// When the character is not grounded, play jump-animation. Which only plays frames 20-27 once and then resets when the character is grounded.
    /// </summary>
    private void PlayJumpAnimation()
    {
        if (animationTimer >= animationInterval)
        {
            animationTimer = 0f;
            spriteComponent.CurrentFrame = 21 + jumpFrame;
            
            if (jumpFrame < 7 - 1)
            {
                jumpFrame++;
            }
        }
    }
}
