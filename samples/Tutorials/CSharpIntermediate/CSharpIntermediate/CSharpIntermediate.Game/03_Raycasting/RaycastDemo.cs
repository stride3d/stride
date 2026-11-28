// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using CSharpIntermediate.Code.Extensions;
using Stride.BepuPhysics;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace CSharpIntermediate.Code
{
    public class RaycastDemo : SyncScript
    {
        public CollisionMask CollideWithGroup = CollisionMask.Everything;
        public Entity HitPoint;

        private const float maxDistance = 4.0f;
        private Entity laser;
        private BepuSimulation simulation;
      
        public override void Start()
        {
            //Store the physics simulation object
            simulation = Entity.GetSimulation();
            laser = Entity.FindChild("Laser");
        }

        public override void Update()
        {
            int drawX = 340;
            int drawY = 80;
            DebugText.Print("Press Q and E to raise/lower weapons", new Int2(drawX, drawY));

            var raycastStart = Entity.Transform.Position;
            var raycastEnd = Entity.Transform.Position + new Vector3(0, 0, maxDistance);
          
            drawY += 40;

            // Send a raycast from the start to the endposition
            if (simulation.RayCast(raycastStart, Vector3.UnitZ, maxDistance, out HitInfo hitResult, CollideWithGroup))
            {
                // If we hit something, calculate the distance to the hitpoint and scale the laser to that distance
                HitPoint.Transform.Position = hitResult.Point;
                var distance = Vector3.Distance(hitResult.Point, raycastStart);
                laser.Transform.Scale.Z = distance;

                DebugText.Print("Hit a collider", new Int2(drawX, drawY));
                DebugText.Print($"Raycast hit distance: {distance}", new Int2(drawX, drawY + 20));
                DebugText.Print($"Raycast hit point: {hitResult.Point.Print()}", new Int2(drawX, drawY + 40));
                DebugText.Print($"Raycast hit entity: {hitResult.Collidable.Entity.Name}", new Int2(drawX, drawY + 60));
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
