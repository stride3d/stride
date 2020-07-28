using Stride.Core.Mathematics;
using Stride.Engine;

namespace CSharpBeginner.Code
{
    /// <summary>
    /// This script demonstrates how we can instantiate prefabs
    /// </summary>
    public class InstantiatingPrefabsDemo : SyncScript
    {
        public Prefab PileOfBoxesPrefab;
        public override void Start()
        {
            // A prefab can be instantiated. Is does not give you a new prefab, but instead gives you a list of entities
            var pileOfBoxesInstance = PileOfBoxesPrefab.Instantiate();

            // An instantiated prefab does nothing and isn't visible untill we add it to the scene
            Entity.Scene.Entities.AddRange(pileOfBoxesInstance);


            // We can also load a prefab by using the Content.Load method
            var pileOfBoxesPrefabFromContent = Content.Load<Prefab>("Prefabs/Pile of boxes");
            var pileOfBoxesInstance2 = pileOfBoxesPrefabFromContent.Instantiate();

            // We add the entities to a new entity that we can use a parent
            // We can easily position and rotate the parent entity
            var pileOfBoxesParent = new Entity(new Vector3(0, 0, -2), "PileOfBoxes2");
            pileOfBoxesParent.Transform.Rotation = Quaternion.RotationY(135);
            foreach (var entity in pileOfBoxesInstance2)
            {
                pileOfBoxesParent.AddChild(entity);
            }
            Entity.Scene.Entities.Add(pileOfBoxesParent);
        }

        public override void Update()
        {
            DebugText.Print("The original prefab", new Int2(310, 320));
            DebugText.Print("The prefab instance PileOfBoxes", new Int2(560, 370));
            DebugText.Print("The prefab instance PileOfBoxes2 with custom parent", new Int2(565, 650));
        }
    }
}
