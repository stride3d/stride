using Stride.Engine;

namespace Stride.BepuPhysics.Components.Car
{
    [ComponentCategory("Bepu - Car")]
    public class WheelComponent : StartupScript
    {
        public float DamperLen { get; set; } = 0.5f;
        public float DamperRatio { get; set; } = 0.01f;
        public float DamperForce { get; set; } = 1000f;
    }
}
