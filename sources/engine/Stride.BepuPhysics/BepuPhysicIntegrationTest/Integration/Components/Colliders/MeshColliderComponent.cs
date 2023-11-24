using BepuPhysicIntegrationTest.Integration.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace BepuPhysicIntegrationTest.Integration.Components.Colliders
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ColliderProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Colliders")]
    public class MeshColliderComponent : ColliderComponent
    {
        public ModelComponent? ModelData { get; set; }

        public MeshColliderComponent()
        {
        }
    }
}
