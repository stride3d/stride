using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Input;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Sprites;

namespace _2DPlatformer.Gameplay;

public class CoinRotation : SyncScript
{
    [Display("Coin Sprite Sheet")]
    public SpriteSheet CoinSprites { get; set; }
    private SpriteFromSheet spriteComponent;
    
    private double animationTimer = 0f;
    private readonly float animationInterval = 1f / 12f;
    
    public override void Start()
    {
        spriteComponent = Entity.Get<SpriteComponent>().SpriteProvider as SpriteFromSheet;
    }

    public override void Update()
    {
        animationTimer += Game.UpdateTime.Elapsed.TotalSeconds;
        
        if (animationTimer >= animationInterval)
        {
            animationTimer = 0f;
            spriteComponent.CurrentFrame = (spriteComponent.CurrentFrame + 1) % 12;
        }
    }
}
