// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Threading.Tasks;
using Stride.Engine;
using Stride.Physics;

namespace SpriteStudioDemo
{
    public class BeamScript : AsyncScript
    {
        private const float maxWidthX = 8f + 2f;
        private const float minWidthX = -8f - 2f;

        private bool dead;

        public void Die()
        {
            dead = true;
        }

        public override async Task Execute()
        {
            while(Game.IsRunning)
            {
                await Script.NextFrame();

                if ((Entity.Transform.Position.X <= minWidthX) || (Entity.Transform.Position.X >= maxWidthX) || dead)
                {
                    SceneSystem.SceneInstance.RootScene.Entities.Remove(Entity);
                    return;
                }
            }
        }
    }
}
