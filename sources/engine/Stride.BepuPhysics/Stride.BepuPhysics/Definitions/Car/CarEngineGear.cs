using Stride.Core;

namespace Stride.BepuPhysics.Definitions.Car
{
    [DataContract]
    public class CarEngineGear
    {
        public float AccelerationForce { get; set; }
        public float GearRatio { get; set; }

    }
}
