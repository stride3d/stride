using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Physics;

namespace CSharpIntermediate.Code
{
    public class RaycastDemo : SyncScript
    {
        public CollisionFilterGroupFlags CollideWithGroup;
        public bool CollideWithTriggers = false;
        public Entity HitPoint;

        private const float maxDistance = 4.0f;
        private Entity laser;
        private Simulation simulation;
      
        public override void Start()
        {
            //Store the physics simulation object
            simulation = this.GetSimulation();
            laser = Entity.FindChild("Laser");
        }

        public override void Update()
        {
            int drawX = 40;
            int drawY = 80;
            DebugText.Print("Raycast demo", new Int2(drawX, drawY));

            var raycastStart = Entity.Transform.Position;
            var raycastEnd = Entity.Transform.Position + new Vector3(0, 0, maxDistance);
          
            drawY += 40;

            // Send a raycast from the start to the endposition
            if (simulation.Raycast(raycastStart, raycastEnd, out HitResult hitResult, CollisionFilterGroups.DefaultFilter, CollideWithGroup, CollideWithTriggers))
            {
                // If we hit something, calculate the distance to the hitpoint and scale the laser to that distance
                HitPoint.Transform.Position = hitResult.Point;
                var distance = Vector3.Distance(hitResult.Point, raycastStart);
                laser.Transform.Scale.Z = distance;

                DebugText.Print("Hit a collider", new Int2(drawX, drawY));
                DebugText.Print($"Raycast hit distance: {distance}", new Int2(drawX, drawY + 20));
                DebugText.Print($"Raycast hit point: {hitResult.Point}", new Int2(drawX, drawY + 40));
                DebugText.Print($"Raycast hit entity: {hitResult.Collider.Entity.Name}", new Int2(drawX, drawY + 60));
            }
            else
            {
                // If we didn't hit anything, scale the laser to match the distance between start and end
                HitPoint.Transform.Position = raycastEnd;
                laser.Transform.Scale.Z = Vector3.Distance(raycastStart, raycastEnd);
                DebugText.Print("No collider hit", new Int2(drawX, drawY));
            }
        }
    }
}
