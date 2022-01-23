using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Physics;

namespace CSharpIntermediate.Code
{
    public class RaycastDemo : SyncScript
    {
        public float RotationSpeed = 0.4f;
        public float MaxDistance = 4.0f;
        Entity barrel;
        Entity laser;
        Entity hitPointVisualiser;
        Simulation simulation;

        public override void Start()
        {
            barrel = Entity.FindChild("Barrel");
            laser = Entity.FindChild("Laser");
            hitPointVisualiser = Entity.FindChild("HitpointVisualiser");

            simulation = this.GetSimulation();
        }

        public override void Update()
        {
            RotateWeapon();

            hitPointVisualiser.Transform.Position = barrel.Transform.Position + new Vector3(0, 0, MaxDistance);
            var raycastEndWorldPosition = hitPointVisualiser.Transform.WorldMatrix.TranslationVector;
            var barrelWorldPosition = barrel.Transform.WorldMatrix.TranslationVector;
            var result = simulation.Raycast(barrelWorldPosition, raycastEndWorldPosition);

            if (result.Succeeded)
            {
                var length = Vector3.Distance(barrelWorldPosition, result.Point);
                laser.Transform.Scale = new Vector3(0.01f, length, 1);

                // Update the position of the hit point visualiser
                hitPointVisualiser.Transform.WorldMatrix.TranslationVector = result.Point;
                hitPointVisualiser.Transform.UpdateWorldMatrix();

                DebugText.Print("Hit collider", new Int2(500, 200));
                DebugText.Print("Raycast hit point: " + result.Point.ToString(), new Int2(500, 220));
                DebugText.Print("Raycast hit entity : " + result.Collider.Entity.Name, new Int2(500, 240));
            }
            else
            {
                // The length of the raycast is similar to MaxDistance
                var length = Vector3.Distance(barrelWorldPosition, raycastEndWorldPosition);
                laser.Transform.Scale = new Vector3(0.01f, length, 1);
                DebugText.Print("No collider hit", new Int2(500, 220));
            }
        }

        private void RotateWeapon()
        {
            DebugText.Print("Press Q and E to rotate the weapon", new Int2(500, 180));
            var delta = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            if (Input.IsKeyDown(Keys.Q))
            {
                Entity.Transform.Rotation *= Quaternion.RotationY(RotationSpeed * delta);
            }

            if (Input.IsKeyDown(Keys.E))
            {
                Entity.Transform.Rotation *= Quaternion.RotationY(-RotationSpeed * delta);
            }
        }
    }
}
