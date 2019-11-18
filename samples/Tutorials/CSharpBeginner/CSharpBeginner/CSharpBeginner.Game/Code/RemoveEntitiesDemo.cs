using Xenko.Core.Mathematics;
using Xenko.Engine;

namespace CSharpBeginner.Code
{
    /// <summary>
    /// This script demonstrates how to remove an existing entity from the scene hierarchy.
    /// </summary>
    public class RemoveEntitiesDemo : SyncScript
    {
        public Entity EntityToClone;
        private Entity clonedEntity1;
        private float cloneCounter = 0;
        private float timer = 0;
        private float createAndRemoveTime = 2;

        public override void Start()
        {
            CloneEntityAndAddToScene();
        }

        /// <summary>
        /// This methods clones an entity, adds it to the scene and increases a counter
        /// </summary>
        private void CloneEntityAndAddToScene()
        {
            clonedEntity1 = EntityToClone.Clone();
            clonedEntity1.Transform.Position += new Vector3(0, 0, -0.5f);
            Entity.Scene.Entities.Add(clonedEntity1);
            cloneCounter++;
        }

        public override void Update()
        {
            // We use a simple timer
            timer += (float)Game.UpdateTime.Elapsed.TotalSeconds;
            if (timer > createAndRemoveTime)
            {
                // If the clonedEntity variable is null, we clone an entity and add it to the scene
                if (clonedEntity1 == null)
                {
                    CloneEntityAndAddToScene();
                }
                else
                {
                    // We remove the cloned entity from the scene 
                    Entity.Scene.Entities.Remove(clonedEntity1);

                    // We also need to set it to null, otherwise the clonedEntity still exists
                    clonedEntity1 = null;
                }

                // Reset timer
                timer = 0;
            }

            DebugText.Print("Every uneven second we clone an entity and add it to the scene.", new Int2(400, 320));
            DebugText.Print("Every even second we remove the cloned entity from the scene.", new Int2(400, 340));
            DebugText.Print("Clone counter: " + cloneCounter, new Int2(400, 360));
            if (clonedEntity1 == null)
            {
                DebugText.Print("Cloned entity is null", new Int2(550, 500));
            }
            else
            {
                DebugText.Print("Cloned entity is in the scene", new Int2(550, 500));
            }
        }
    }
}
