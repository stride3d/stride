using System;
using Stride.Core;
using System.Threading.Tasks;
using Stride.Engine;
using Stride.Audio;
using Stride.Physics;
using _2DPlatformer.Player;

namespace _2DPlatformer.Gameplay;

public class CollectCoin : AsyncScript
{
    private StaticColliderComponent staticColliderComponent;
    
    [Display("Collect Coin")]
    public Sound SoundEffect { get; set; }
    private SoundInstance sfxInstance;

    public override async Task Execute()
    {
        staticColliderComponent = Entity.Get<StaticColliderComponent>();
    
        while(Game.IsRunning)
        {
            // Waits for a Collision with the Character
            var collision = await staticColliderComponent.NewCollision();
            var otherCollider = staticColliderComponent == collision.ColliderA ? collision.ColliderB : collision.ColliderA;
            
            if (otherCollider.Entity.Get<PlayerController>() != null)
            {
                // Disable Sprite
                Entity.Get<SpriteComponent>().Enabled = false;
                sfxInstance = SoundEffect?.CreateInstance();
                sfxInstance?.Stop();
                 
                // Play Sound
                sfxInstance?.Play();
                 
                // Disable Coin Object
                var task = Task.Run(() => RemoveEntityAfterWaiting());
                task.Wait(TimeSpan.FromSeconds(3));
            }
        }
    }
    
    /// <summary>
    /// Deactivate the object.
    /// </summary>
    private void RemoveEntityAfterWaiting()
    {
        Entity.Scene.Entities.Remove(Entity);
    }
}
