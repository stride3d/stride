using Stride.BepuPhysics.Extensions;
using Stride.Engine;
using Stride.Input;

#warning This should not be part of the base API, move it to demo/sample

namespace Stride.BepuPhysics.Demo.Components.Utils
{
    //[DataContract("SpawnerComponent", Inherited = true)]
    [ComponentCategory("BepuDemo - Utils")]
    public class ThrowerComponent : Spawner
    {
        public Entity? SpawnPosition { get; set; }

        public float Speed { get; set; } = 20f;

        public override void SimulationUpdate(float simTimeStep)
        {
        }

        public override void Update()
        {
            DebugText.Print("Throw a prefab (T)", new(Game.Window.PreferredWindowedSize.X - 500, 125));

            if (SpawnPosition == null) return;

            if (Input.IsKeyPressed(Keys.T))
            {
                var camera = Game.Services.GetService<SceneSystem>().GraphicsCompositor.Cameras[0].Camera;
                var forward = Core.Mathematics.Vector3.TransformNormal(-Core.Mathematics.Vector3.UnitZ, Core.Mathematics.Matrix.RotationQuaternion(camera.Entity.Transform.GetWorldRot())).ToNumericVector();

                Spawn(SpawnPosition.Transform.GetWorldPos(), (forward * Speed).ToStrideVector(), new());
            }
        }
    }
}
