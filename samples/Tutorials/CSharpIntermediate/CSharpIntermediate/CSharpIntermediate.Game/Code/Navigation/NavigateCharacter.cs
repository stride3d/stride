using System;
using System.Collections.Generic;
using CSharpIntermediate.Code.Extensions;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Input;
using Stride.Navigation;
using Stride.Physics;

namespace CSharpIntermediate.Code
{
    public class NavigateCharacter : SyncScript
    {
        public Entity RegularCharacter;
        public Entity PathSphere;
        public float MovementSpeed;

        private NavigationComponent navigationComponent;
        private List<Vector3> waypoints = new();
        private List<Entity> wayPointSpheres = new();

        private int waypointIndex = 0;


        public override void Start()
        {
            navigationComponent = RegularCharacter.Get<NavigationComponent>();
        }

        public override void Update()
        {
            DebugText.Print($"Left click to set Regular character target", new Int2(200, 20));
            if (Input.IsMouseButtonPressed(MouseButton.Left))
            {
                CleanupExistingPath();
                SetTarget();
            }

            UpdateMovement();
        }

        private void UpdateMovement()
        {
            if (waypoints.Count == 0)
            {
                DebugText.Print($"No target", new Int2(200, 60));
                return;
            }

            var deltaTime = (float)Game.UpdateTime.Elapsed.TotalSeconds;
            var curPosition = RegularCharacter.Transform.WorldMatrix.TranslationVector;
            var nextWaypointPosition = waypoints[waypointIndex];
            var distanceToWaypoint = Vector3.Distance(curPosition, nextWaypointPosition);

            DebugText.Print($"Distance to waypoint {distanceToWaypoint.ToString("0.0")} ", new Int2(200, 60));
            if (distanceToWaypoint > 0.1)
            {
                var direction = nextWaypointPosition - curPosition;
                direction.Normalize();
                direction *= MovementSpeed * deltaTime;

                RegularCharacter.Transform.Position += direction;
            }
            else
            {
                if(waypointIndex+1 < waypoints.Count)
                {
                    waypointIndex++;
                }
                else
                {
                    CleanupExistingPath();
                }
            }
        }

        private void SetTarget()
        {
            var backBuffer = GraphicsDevice.Presenter.BackBuffer;
            var viewport = new Viewport(0, 0, backBuffer.Width, backBuffer.Height);
            var camera = Entity.Get<CameraComponent>();
            var nearPosition = viewport.Unproject(new Vector3(Input.AbsoluteMousePosition, 0.0f), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);
            var farPosition = viewport.Unproject(new Vector3(Input.AbsoluteMousePosition, 1.0f), camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);

            var hitResult = this.GetSimulation().Raycast(nearPosition, farPosition);

            if (hitResult.Succeeded)
            {
                if (navigationComponent.TryFindPath(hitResult.Point, waypoints))
                {
                    waypointIndex = 0;
                    foreach (var waypoint in waypoints)
                    {
                        var waypointSphere = PathSphere.Clone();
                        waypointSphere.Transform.Position = waypoint;

                        wayPointSpheres.Add(waypointSphere);
                        Entity.Scene.Entities.Add(waypointSphere);
                    }
                }
            }
        }

        private void CleanupExistingPath()
        {
            foreach (var waypointSphere in wayPointSpheres)
            {
                Entity.Scene.Entities.Remove(waypointSphere);
            }
            wayPointSpheres.Clear();
            waypoints.Clear();
        }
    }
}
