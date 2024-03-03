using Stride.Engine;

#warning This need rework/Rename and could be part of the API

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
