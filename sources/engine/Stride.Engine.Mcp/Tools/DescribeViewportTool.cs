// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Processors;
using Stride.Graphics;

namespace Stride.Engine.Mcp.Tools
{
    [McpServerToolType]
    public sealed class DescribeViewportTool
    {
        [McpServerTool(Name = "describe_viewport"), Description("Returns a list of entities visible in the game viewport with their projected screen coordinates (normalized 0-1). This helps understand what is rendered even when materials or lighting make the image hard to interpret. Use alongside capture_screenshot to correlate visual output with entity positions.")]
        public static async Task<string> DescribeViewport(
            GameBridge bridge,
            [Description("Maximum number of entities to return (default 100)")] int maxEntities = 100,
            CancellationToken cancellationToken = default)
        {
            var result = await bridge.RunOnGameThread(game =>
            {
                var rootScene = game.SceneSystem?.SceneInstance?.RootScene;
                if (rootScene == null)
                    return (object)new { error = "No scene loaded" };

                // Find the active camera
                CameraComponent activeCamera = null;
                FindActiveCamera(rootScene, ref activeCamera);

                if (activeCamera == null)
                    return (object)new { error = "No active camera found in the scene" };

                // Get the viewport
                var viewport = game.GraphicsContext.CommandList.Viewport;
                var viewProjection = activeCamera.ViewProjectionMatrix;

                // Use a normalized viewport for 0-1 screen coordinates
                var normalizedViewport = new Viewport(0, 0, 1, 1);

                // Collect all entities with their screen projections
                var entities = new List<EntityScreenInfo>();
                CollectEntities(rootScene, activeCamera, normalizedViewport, entities);

                // Sort by distance to camera (nearest first), then cap
                entities.Sort((a, b) => a.DistanceToCamera.CompareTo(b.DistanceToCamera));
                if (entities.Count > maxEntities)
                    entities.RemoveRange(maxEntities, entities.Count - maxEntities);

                // Extract camera world position from view matrix
                var viewMatrix = activeCamera.ViewMatrix;
                Matrix.Invert(ref viewMatrix, out var cameraWorldMatrix);
                var camPos = cameraWorldMatrix.TranslationVector;

                return (object)new Dictionary<string, object?>
                {
                    ["camera"] = new Dictionary<string, object?>
                    {
                        ["position"] = new { x = Math.Round(camPos.X, 2), y = Math.Round(camPos.Y, 2), z = Math.Round(camPos.Z, 2) },
                        ["projection"] = activeCamera.Projection == CameraProjectionMode.Perspective ? "perspective" : "orthographic",
                        ["fieldOfViewDegrees"] = Math.Round(activeCamera.VerticalFieldOfView, 1),
                        ["nearPlane"] = Math.Round(activeCamera.NearClipPlane, 3),
                        ["farPlane"] = Math.Round(activeCamera.FarClipPlane, 1),
                        ["aspectRatio"] = Math.Round(activeCamera.AspectRatio, 3),
                    },
                    ["totalEntitiesInScene"] = CountEntities(rootScene),
                    ["visibleEntityCount"] = entities.Count(e => e.IsVisible),
                    ["entities"] = entities.Select(e => (object)new Dictionary<string, object?>
                    {
                        ["id"] = e.Id,
                        ["name"] = e.Name,
                        ["worldPosition"] = new { x = Math.Round(e.WorldPosition.X, 2), y = Math.Round(e.WorldPosition.Y, 2), z = Math.Round(e.WorldPosition.Z, 2) },
                        ["screenPosition"] = e.IsVisible
                            ? (object)new { x = Math.Round(e.ScreenX, 3), y = Math.Round(e.ScreenY, 3) }
                            : null,
                        ["distanceToCamera"] = Math.Round(e.DistanceToCamera, 2),
                        ["isVisible"] = e.IsVisible,
                        ["components"] = e.Components,
                    }).ToList(),
                };
            }, cancellationToken);

            return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
        }

        private static void FindActiveCamera(Scene scene, ref CameraComponent camera)
        {
            foreach (var entity in scene.Entities)
            {
                FindCameraInEntity(entity, ref camera);
                if (camera != null) return;
            }

            foreach (var child in scene.Children)
            {
                FindActiveCamera(child, ref camera);
                if (camera != null) return;
            }
        }

        private static void FindCameraInEntity(Entity entity, ref CameraComponent camera)
        {
            var cam = entity.Get<CameraComponent>();
            if (cam != null && cam.Enabled)
            {
                camera = cam;
                return;
            }

            if (entity.Transform != null)
            {
                foreach (var child in entity.Transform.Children)
                {
                    if (child.Entity != null)
                    {
                        FindCameraInEntity(child.Entity, ref camera);
                        if (camera != null) return;
                    }
                }
            }
        }

        private static void CollectEntities(Scene scene, CameraComponent camera, Viewport viewport, List<EntityScreenInfo> results)
        {
            foreach (var entity in scene.Entities)
            {
                CollectEntityRecursive(entity, camera, viewport, results);
            }

            foreach (var child in scene.Children)
            {
                CollectEntities(child, camera, viewport, results);
            }
        }

        private static void CollectEntityRecursive(Entity entity, CameraComponent camera, Viewport viewport, List<EntityScreenInfo> results)
        {
            var worldPos = entity.Transform.WorldMatrix.TranslationVector;

            // Get camera position for distance calculation
            var viewMatrix = camera.ViewMatrix;
            Matrix.Invert(ref viewMatrix, out var cameraWorldMatrix);
            var camPos = cameraWorldMatrix.TranslationVector;
            var distance = Vector3.Distance(worldPos, camPos);

            // Project to screen space
            var screenPos = viewport.Project(worldPos, camera.ProjectionMatrix, camera.ViewMatrix, Matrix.Identity);

            // Check visibility: in front of camera and within screen bounds
            bool isVisible = screenPos.Z >= 0 && screenPos.Z <= 1
                && screenPos.X >= -0.5f && screenPos.X <= 1.5f
                && screenPos.Y >= -0.5f && screenPos.Y <= 1.5f;

            results.Add(new EntityScreenInfo
            {
                Id = entity.Id.ToString(),
                Name = entity.Name ?? "(unnamed)",
                WorldPosition = worldPos,
                ScreenX = screenPos.X,
                ScreenY = screenPos.Y,
                DistanceToCamera = distance,
                IsVisible = isVisible,
                Components = entity.Components.Select(c => c.GetType().Name).ToList(),
            });

            // Recurse into children
            if (entity.Transform != null)
            {
                foreach (var childTransform in entity.Transform.Children)
                {
                    if (childTransform.Entity != null)
                    {
                        CollectEntityRecursive(childTransform.Entity, camera, viewport, results);
                    }
                }
            }
        }

        private static int CountEntities(Scene scene)
        {
            int count = 0;
            foreach (var entity in scene.Entities)
            {
                count += CountEntityRecursive(entity);
            }
            foreach (var child in scene.Children)
            {
                count += CountEntities(child);
            }
            return count;
        }

        private static int CountEntityRecursive(Entity entity)
        {
            int count = 1;
            if (entity.Transform != null)
            {
                foreach (var childTransform in entity.Transform.Children)
                {
                    if (childTransform.Entity != null)
                        count += CountEntityRecursive(childTransform.Entity);
                }
            }
            return count;
        }

        private class EntityScreenInfo
        {
            public string Id;
            public string Name;
            public Vector3 WorldPosition;
            public float ScreenX;
            public float ScreenY;
            public float DistanceToCamera;
            public bool IsVisible;
            public List<string> Components;
        }
    }
}
