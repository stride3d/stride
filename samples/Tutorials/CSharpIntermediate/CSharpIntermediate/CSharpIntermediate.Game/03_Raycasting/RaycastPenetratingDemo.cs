// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core.Mathematics;
using Stride.BepuPhysics;
using Stride.Engine;

namespace CSharpIntermediate.Code
{
    public class RaycastPenetratingDemo : SyncScript
    {
        public CollisionMask CollideWithGroup = CollisionMask.Everything;

        private Entity laser;
        private const float maxDistance = 3.5f;
        private BepuSimulation simulation;

        public override void Start()
        {
            simulation = Entity.GetSimulation();
            laser = Entity.FindChild("Laser");
        }

        public override void Update()
        {
            int drawX = 700;
            int drawY = 80;
            DebugText.Print("Raycast penetration demo", new Int2(drawX, drawY));

            var raycastStart = Entity.Transform.Position;
            var raycastEnd = Entity.Transform.Position + new Vector3(0, 0, -maxDistance);

            var distance = Vector3.Distance(raycastStart, raycastEnd);
            laser.Transform.Scale.Z = distance;

            var hitResults = new List<HitInfo>();
            simulation.RayCastPenetrating(raycastStart, -Vector3.UnitZ, maxDistance, hitResults, CollideWithGroup);

            drawY += 40;
            if (hitResults.Count > 0)
            {
                DebugText.Print($"Raycast has hit {hitResults.Count} object(s)", new Int2(drawX, drawY));

                foreach (var hitResult in hitResults)
                {
                    drawY += 20;
                    DebugText.Print($"- Raycast has hit: {hitResult.Collidable.Entity.Name}", new Int2(drawX, drawY));
                }
            }
            else
            {
                DebugText.Print("No collider hit", new Int2(drawX, drawY));
            }
        }
    }
}
