using Stride.Engine;

#warning I can see this being useful, but it would have to be far more flexible and probably its own project, so maybe move this to demo/sample for now

namespace Stride.BepuPhysics.Demo.Components.Car
{
    [ComponentCategory("BepuDemo - Car")]
    public class WheelComponent : StartupScript
    {
        public float DamperLen { get; set; } = 0.5f;
        public float DamperRatio { get; set; } = 0.01f;
        public float DamperForce { get; set; } = 1000f;
    }
}
