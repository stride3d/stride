using Xenko.Core.Mathematics;
using Xenko.Engine;

namespace CSharpBeginner.Code
{
    /// <summary>
    /// This script demonstrates how to access the entity's local and world position and displays them on screen. 
    /// </summary>
    public class TransformPositionDemo : SyncScript
    {
        public override void Start() { }

        public override void Update()
        {
            // We store the local and world position of our entity's tranform in a Vector3 variable
            Vector3 localPosition = Entity.Transform.Position;
            Vector3 worldPosition = Entity.Transform.WorldMatrix.TranslationVector;

            // We disaply the entity's name and its local and world position on screen
            DebugText.Print(Entity.Name + " - local position: " + localPosition, new Int2(400, 450));
            DebugText.Print(Entity.Name + " - world position: " + worldPosition, new Int2(400, 470));
        }
    }
}
