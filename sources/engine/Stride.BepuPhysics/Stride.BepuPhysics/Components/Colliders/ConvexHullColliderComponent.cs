using Stride.BepuPhysics.Processors;
using Stride.Core;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Components.Colliders
{
    [DataContract]
    [DefaultEntityComponentProcessor(typeof(ColliderProcessor), ExecutionMode = ExecutionMode.Runtime)]
    [ComponentCategory("Bepu - Colliders")]
    public class ConvexHullColliderComponent : ColliderComponent
    {
        public ModelComponent? ModelData { get; set; }

        public ConvexHullColliderComponent()
        {
        }
    }
}
