using BepuPhysicIntegrationTest.Integration.Processors;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace BepuPhysicIntegrationTest.Integration.Components.Colliders
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ColliderProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Colliders")]
    public class BoxColliderComponent : ColliderComponent
    {
        public Vector3 Size = new(1, 1, 1);

        public override void Update()
        {
        }
    }
}
