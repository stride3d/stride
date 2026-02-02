using System;
using Stride.Core;
using System.Threading.Tasks;
using Stride.Engine;
using Stride.Audio;
using Stride.Physics;
using Platformer2D.Player;

namespace Platformer2D.Gameplay;

public class CollectCoin : AsyncScript
{
    public required StaticColliderComponent StaticColliderComponent { get; init; }
    
    [Display("Collect Coin")]
    public Sound SoundEffect { get; set; }
    private SoundInstance? sfxInstance;

    public override async Task Execute()
    {
        while(Game.IsRunning)
        {
            // Waits for a Collision with the Character
            var collision = await StaticColliderComponent.NewCollision();
            var otherCollider = StaticColliderComponent == collision.ColliderA ? collision.ColliderB : collision.ColliderA;
            
            if (otherCollider.Entity.Get<PlayerController>() != null)
            {
                // Disable Sprite
                Entity.Get<SpriteComponent>().Enabled = false;
                sfxInstance = SoundEffect?.CreateInstance();
                sfxInstance?.Stop();
                 
                // Play Sound
                sfxInstance?.Play();
                 
                // Disable Coin Object
                await Task.Delay(TimeSpan.FromSeconds(3));
                Entity.Scene = null;
            }
        }
    }
}
