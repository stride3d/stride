using Stride.Core.Mathematics;
using Stride.Engine;

namespace CSharpBeginner.Code
{
    /// <summary>
    /// This script demonstrates how to remove an existing entity from a scene and an entity that is a child of an entity.
    /// </summary>
    public class RemoveEntitiesDemo : SyncScript
    {
        public Entity EntityToClone;

        private Entity clonedEntity1;
        private Entity clonedEntity2;
        private float timer = 0;
        private float existTime = 5;
        private float goneTime = 2;
        private bool entitiesExist = false;

        public override void Start()
        {
            CloneEntityAndAddAsChild();
            CloneEntityAndAddToScene();
            entitiesExist = true;
        }

        /// This methods clones an entity, adds it as a child of the current entity
        private void CloneEntityAndAddAsChild()
        {
            clonedEntity1 = EntityToClone.Clone();
            clonedEntity1.Transform.Position = new Vector3(0);
            Entity.AddChild(clonedEntity1);
        }

        /// This methods clones an entity, adds it to the scene root
        private void CloneEntityAndAddToScene()
        {
            clonedEntity2 = EntityToClone.Clone();
            clonedEntity2.Transform.Position += new Vector3(0, 0, 1);
            Entity.Scene.Entities.Add(clonedEntity2);
        }

        public override void Update()
        {
            timer += (float)Game.UpdateTime.Elapsed.TotalSeconds;
            if (timer > (entitiesExist ? existTime : goneTime))
            {
                // If the clonedEntities are null, we clone new entities
                if (clonedEntity1 == null && clonedEntity2 == null)
                {
                    CloneEntityAndAddAsChild();
                    CloneEntityAndAddToScene();
                    entitiesExist = true;
                }
                else
                {
                    // We remove the cloned entity that is a child of the current entity
                    Entity.RemoveChild(clonedEntity1);

                    // We remove the cloned entity from the scene 
                    Entity.Scene.Entities.Remove(clonedEntity2);

                    // We also need to set it to null in our script, otherwise the clonedEntities still exists
                    clonedEntity1 = null;
                    clonedEntity2 = null;

                    entitiesExist = false;
                }

                timer = 0;
            }

            DebugText.Print("For " + existTime.ToString() + " seconds: ", new Int2(860, 240));
            DebugText.Print("- Clone 1 is a child of the script entity", new Int2(860, 260));
            DebugText.Print("- Clone 2 is a child of the scene root", new Int2(860, 280));
            DebugText.Print("For " + goneTime.ToString() + " seconds, the cloned entities are gone", new Int2(860, 300));

            if (clonedEntity1 == null && clonedEntity2 == null)
            {
                DebugText.Print("Cloned entity 1 and 2 have been removed", new Int2(450, 600));
            }
            else
            {
                DebugText.Print("Cloned entity 1 is a child of the Script entity", new Int2(450, 350));
                DebugText.Print("Cloned entity 2 is in the scene root", new Int2(450, 600));
            }
        }
    }
}
