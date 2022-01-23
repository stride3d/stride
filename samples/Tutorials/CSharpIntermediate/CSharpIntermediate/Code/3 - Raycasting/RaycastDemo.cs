using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Input;
using Stride.Physics;

namespace CSharpIntermediate.Code
{
    public class RaycastDemo : SyncScript
    {
        public Entity hitPointVisualiser;
        public Entity laser;

        private float MaxDistance = 6.0f;
        private Simulation simulation;

        private bool collideWithTriggers = false;
        private bool collideWithCustomFilter = false;

        public override void Start()
        {
            simulation = this.GetSimulation();
        }

        public override void Update()
        {
            DebugText.Print($"C to toggle colliding with CustomFilter1 : {collideWithCustomFilter}", new Int2(20, 20));
            if (Input.IsKeyPressed(Keys.C))
            {
                collideWithCustomFilter = !collideWithCustomFilter;
            }

            DebugText.Print($"T to toggle colliding with Triggers: {collideWithTriggers}", new Int2(20, 40));
            if (Input.IsKeyPressed(Keys.T))
            {
                collideWithTriggers = !collideWithTriggers;
            }

            var raycastStartPosition = Entity.Transform.Position;
            var raycastEndPosition = raycastStartPosition + new Vector3(0, 0, MaxDistance);

            var colliderGroup = collideWithCustomFilter ? CollisionFilterGroups.CustomFilter1 : CollisionFilterGroups.AllFilter;
            var colliderFlag = collideWithCustomFilter ? CollisionFilterGroupFlags.CustomFilter1 : CollisionFilterGroupFlags.AllFilter;
            var hitResult = simulation.Raycast(raycastStartPosition, raycastEndPosition, colliderGroup, colliderFlag, collideWithTriggers);

            hitPointVisualiser.Transform.Position = new Vector3(0);
            laser.Transform.Scale.Z = 0;

            if (hitResult.Succeeded)
            {

                hitPointVisualiser.Transform.Position = hitResult.Point;
                var distance = Vector3.Distance(hitResult.Point, raycastStartPosition);
                laser.Transform.Scale.Z = distance;

                DebugText.Print("Hit a collider", new Int2(500, 20));
                DebugText.Print($"Raycast hit distance: {distance}", new Int2(500, 40));
                DebugText.Print($"Raycast hit point: {hitResult.Point}", new Int2(500, 60));
                DebugText.Print($"Raycast hit entity: {hitResult.Collider.Entity.Name}", new Int2(500, 80));
            }
            else
            {
                DebugText.Print("No collider hit", new Int2(500, 220));
            }
        }
    }
}
