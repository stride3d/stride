using BepuPhysics;
using BepuPhysics.Collidables;
using Stride.Core.Mathematics;

namespace Stride.BepuPhysics.Components.Containers.Interfaces
{
    public interface IStaticContainer : IContainer
    {
        public Vector3 Position { get; set; }
        public Quaternion Orientation { get; set; }
        public ContinuousDetection ContinuousDetection { get; set; }
    }
}