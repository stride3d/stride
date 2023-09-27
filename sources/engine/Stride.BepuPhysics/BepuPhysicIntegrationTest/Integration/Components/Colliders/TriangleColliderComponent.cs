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
    public class TriangleColliderComponent : ColliderComponent
    {
        public Vector3 A = new(1, 1, 1);
        public Vector3 B = new(1, 1, 1);
        public Vector3 C = new(1, 1, 1);
        //TODO
        public TriangleColliderComponent()
        {
        }
    }
}
