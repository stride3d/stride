using System;
using Stride.Core;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering.Sprites;

namespace Platformer2D.Gameplay;

public class CoinRotation : SyncScript
{
    public required SpriteComponent Sprite { get; init; }

    private float animationTimer = 0f;
    // Animation Spped: Every 1/12 passed seconds the next frame will be played. Closer to 1.0 is a slower Animation.
    private const float animationInterval = 1f / 12f;
    private const int COIN_FRAMES_END = 12;

    private SpriteFromSheet SpriteSheet => (SpriteFromSheet)Sprite.SpriteProvider;

    public override void Update()
    {
        animationTimer += (float) Game.UpdateTime.Elapsed.TotalSeconds;
        
        if (animationTimer >= animationInterval)
        {
            animationTimer -= animationInterval;
            SpriteSheet.CurrentFrame = (SpriteSheet.CurrentFrame + 1) % COIN_FRAMES_END;
        }
    }
}
