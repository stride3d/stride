using Xenko.Core.Mathematics;
using Xenko.Engine;

namespace CSharpBeginner.Code
{
    /// <summary>
    /// This script demonstrates how to clone an existing entity.
    /// Cloned entities can be added to the scene hierarchy.
    /// </summary>
    public class CloneEntityDemo : SyncScript
    {
        public Entity MasterSword;
        Entity clone0;
        Entity clone1;
        Entity clone2;

        public override void Start()
        {
            // Clone 0
            // The Clone method clones an existing entity. 
            // However, if we don't add it to the scene, we will never get to see it.
            clone0 = MasterSword.Clone();
            clone0.Transform.Position += new Vector3(0, 1, 0);


            // Clone 1
            clone1 = MasterSword.Clone();

            // We can add Clone1 to the same scene that the current entity is part of
            Entity.Scene.Entities.Add(clone1);

            // The cloned entity will be at the same worldposition as the original Sword entity
            // Move it to the right so that we can see it
            clone1.Transform.Position += new Vector3(-1, 0, 0); 
            clone1.Transform.Scale = new Vector3(1.3f);


            // Clone 2
            clone2 = MasterSword.Clone();

            // We can also add a cloned entity as a child of an existing entity. 
            // That means it will use the parent's world position + parent's local position
            clone2.Transform.Parent = Entity.Transform;

            // Move it the right so that we can see it
            clone2.Transform.Scale = new Vector3(1.6f);
        }

        public override void Update()
        {
            DebugText.Print("Clone 0 has not been added to the scene and is therefore not visible", new Int2(330, 680));
            DebugText.Print("This is the MasterSword, with a Z of 1", new Int2(320, 520));

            DebugText.Print("Clone 1 is placed in the same scene as the entity with the script", new Int2(700, 500));
            DebugText.Print("Clone 1 got the same world position as the 'MasterSword'...", new Int2(700, 520));
            DebugText.Print("... and was then moved to the right", new Int2(700, 540));

            DebugText.Print("Clone 2 is a child of 'MasterSword'.", new Int2(580, 180));
            DebugText.Print("Clone 2 got the same world position + local position of the 'MasterSword'", new Int2(580, 200));
        }
    }
}
