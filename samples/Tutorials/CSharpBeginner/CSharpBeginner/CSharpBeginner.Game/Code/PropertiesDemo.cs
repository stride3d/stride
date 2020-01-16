using System.Collections.Generic;
using Xenko.Core;
using Xenko.Core.Mathematics;
using Xenko.Engine;

namespace CSharpBeginner.Code
{
    /// <summary>
    /// This script demonstrates the most common properties you can expose to the editor.
    /// When we add the public keyword to the variables, they show up as properties in the editor.
    /// Note that in the editor, the properties are alphabetically sorted.
    /// </summary>
    public class PropertiesDemo : SyncScript
    {
        public bool SomeBoolean = true;
        public float SomeFloat = 5.6f;
        public int SomeInteger = 10;
        public string SomeString = "Hello world";
        public Color SomeColor = Color.Red;
        public Vector2 SomeVector2 = new Vector2(1, 2);
        public Vector3 SomeVector3 = new Vector3(1, 2, 3);
        public Vector4 SomeVector4 = new Vector4(1, 2, 3, 4);

        // We can reference other entities to our script by using the Entity class
        public Entity SomeEntity;

        // If we want a list of ojects like strings, integers or even Entities, we have to create the new List right away
        public List<string> StringList = new List<string>();
        public List<Entity> EntityList = new List<Entity>();

        // If we dont want a public property to be visible in the editor we can use '[DataMemberIgnore]'
        [DataMemberIgnore]
        public string SomeHiddenProperty = "HiddenInEditor";

        public override void Update()
        {
            var x = 400;
            DebugText.Print("Integer: " + SomeInteger, new Int2(x, 200));
            DebugText.Print("Float: " + SomeFloat, new Int2(x, 220));
            DebugText.Print("Boolean: " + SomeBoolean, new Int2(x, 240));
            DebugText.Print("String: " + SomeString, new Int2(x, 260));
            DebugText.Print("Vector2: " + SomeVector2, new Int2(x, 280));
            DebugText.Print("Vector3: " + SomeVector3, new Int2(x, 300));
            DebugText.Print("Vector4: " + SomeVector4, new Int2(x, 320));
            DebugText.Print("Color: " + SomeColor, new Int2(x, 340));
            DebugText.Print("Entity: " + SomeEntity.Name, new Int2(x, 360));
            DebugText.Print("String list count: " + StringList.Count, new Int2(x, 380));
            DebugText.Print("Entity list count: " + EntityList.Count, new Int2(x, 400));
        }
    }
}
