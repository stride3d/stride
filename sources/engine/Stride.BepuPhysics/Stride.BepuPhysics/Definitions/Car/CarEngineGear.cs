using Stride.Core;

#warning I can see this being useful, but it would have to be far more flexible and probably its own project, so maybe move this to demo/sample for now

namespace Stride.BepuPhysics.Definitions.Car
{
    [DataContract]
    public class CarEngineGear
    {
        public float AccelerationForce { get; set; }
        public float GearRatio { get; set; }

    }
}
