using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Rendering;

namespace Stride.BepuPhysics.Components.Containers
{
    public interface IStaticContainer : IContainer
    {
        public StaticReference? GetPhysicStatic { get; }
        public Vector3 Position { get; set; }
        public Quaternion Orientation { get; set; }
        public ContinuousDetection ContinuousDetection { get; set; }
    }
}