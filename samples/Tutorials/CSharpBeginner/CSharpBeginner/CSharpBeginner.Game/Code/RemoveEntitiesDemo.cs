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

        private Entity _clonedEntity1;
        private float _cloneCounter = 0;
        private float _timer = 0;
        private float _createAndRemoveTime = 2;

        public override void Start()
        {
            CloneEntityAndAddToScene();
        }

        /// <summary>
        /// This methods clones an entity, adds it to the scene and increases a counter
        /// </summary>
        private void CloneEntityAndAddToScene()
        {
            _clonedEntity1 = EntityToClone.Clone();
            _clonedEntity1.Transform.Position += new Vector3(0, 0, -0.5f);
            Entity.Scene.Entities.Add(_clonedEntity1);
            _cloneCounter++;
        }

        public override void Update()
        {
            // We use a simple timer
            _timer += (float)Game.UpdateTime.Elapsed.TotalSeconds;
            if (_timer > _createAndRemoveTime)
            {
                // If the clonedEntity variable is null, we clone an entity and add it to the scene
                if (_clonedEntity1 == null)
                {
                    CloneEntityAndAddToScene();
                }
                else
                {
                    // We remove the cloned entity from the scene 
                    Entity.Scene.Entities.Remove(_clonedEntity1);

                    // We also need to set it to null, otherwise the clonedEntity still exists
                    _clonedEntity1 = null;
                }

                // Reset timer
                _timer = 0;
            }

            DebugText.Print("Every uneven second we clone an entity and add it to the scene.", new Int2(400, 320));
            DebugText.Print("Every even second we remove the cloned entity from the scene.", new Int2(400, 340));
            DebugText.Print("Clone counter: " + _cloneCounter, new Int2(400, 360));
            if (_clonedEntity1 == null)
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
