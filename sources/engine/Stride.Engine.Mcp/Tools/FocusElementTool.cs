// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ModelContextProtocol.Server;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.Rendering;
using Stride.UI;

namespace Stride.Engine.Mcp.Tools
{
    [McpServerToolType]
    public sealed class FocusElementTool
    {
        [McpServerTool(Name = "focus_element"), Description("Moves the simulated mouse pointer to a UI element or scene entity's screen position. For UI elements, searches all UIComponents in the scene by element name. For entities, projects their world position to screen space using the active camera.")]
        public static async Task<string> FocusElement(
            GameBridge bridge,
            [Description("Target type: 'ui' for UI elements, 'entity' for scene entities")] string target,
            [Description("UI element name (for target='ui')")] string elementName = null,
            [Description("Entity GUID (for target='entity')")] string entityId = null,
            [Description("Entity name (for target='entity')")] string entityName = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await bridge.RunOnGameThread(game =>
                {
                    switch (target?.ToLowerInvariant())
                    {
                        case "ui":
                            return FocusUIElement(game, bridge, elementName);
                        case "entity":
                            return FocusEntity(game, bridge, entityId, entityName);
                        default:
                            return (object)new { error = $"Invalid target type: {target}. Use 'ui' or 'entity'." };
                    }
                }, cancellationToken);

                return JsonSerializer.Serialize(result, new JsonSerializerOptions { WriteIndented = true });
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new { error = $"Focus element failed: {ex.Message}" });
            }
        }

        private static object FocusUIElement(Game game, GameBridge bridge, string elementName)
        {
            if (string.IsNullOrEmpty(elementName))
                return new { error = "elementName is required for UI targeting" };

            var rootScene = game.SceneSystem?.SceneInstance?.RootScene;
            if (rootScene == null)
                return new { error = "No scene loaded" };

            // Search all entities with UIComponent for the named element
            UIElement foundElement = null;
            UIComponent foundComponent = null;

            SearchUIElementInScene(rootScene, elementName, ref foundElement, ref foundComponent);

            if (foundElement == null || foundComponent == null)
                return new { error = $"UI element not found: {elementName}" };

            // Map through UI resolution to screen space
            var resolution = foundComponent.Resolution;
            var backBuffer = game.GraphicsDevice?.Presenter?.BackBuffer;
            if (backBuffer == null)
                return new { error = "Back buffer not available" };

            var screenWidth = (float)backBuffer.Width;
            var screenHeight = (float)backBuffer.Height;

            // Adapt the virtual resolution based on ResolutionStretch mode
            var virtualResolution = new Vector2(resolution.X, resolution.Y);
            switch (foundComponent.ResolutionStretch)
            {
                case ResolutionStretch.FixedWidthAdaptableHeight:
                    virtualResolution.Y = virtualResolution.X * screenHeight / screenWidth;
                    break;
                case ResolutionStretch.FixedHeightAdaptableWidth:
                    virtualResolution.X = virtualResolution.Y * screenWidth / screenHeight;
                    break;
            }

            // The element's WorldMatrix is in centered UI world space, where the root
            // is translated by -virtualResolution/2 (see UIRenderFeature). So WorldMatrix
            // has the element's center offset by -virtualResolution/2 from virtual coords.
            // We undo this to get the position in virtual resolution space (0,0 = top-left).
            var centerWorld = foundElement.WorldMatrix.TranslationVector;
            var virtualPosX = centerWorld.X + virtualResolution.X / 2f;
            var virtualPosY = centerWorld.Y + virtualResolution.Y / 2f;

            var normalizedX = virtualPosX / virtualResolution.X;
            var normalizedY = virtualPosY / virtualResolution.Y;

            // Clamp to 0-1 range
            normalizedX = MathUtil.Clamp(normalizedX, 0f, 1f);
            normalizedY = MathUtil.Clamp(normalizedY, 0f, 1f);

            var screenX = normalizedX * screenWidth;
            var screenY = normalizedY * screenHeight;

            // Move the simulated mouse
            bridge.Mouse.SetPosition(new Vector2(normalizedX, normalizedY));

            return new
            {
                success = true,
                screenX,
                screenY,
                normalizedX,
                normalizedY,
            };
        }

        private static void SearchUIElementInScene(Scene scene, string elementName, ref UIElement foundElement, ref UIComponent foundComponent)
        {
            foreach (var entity in scene.Entities)
            {
                SearchUIElementInEntity(entity, elementName, ref foundElement, ref foundComponent);
                if (foundElement != null) return;
            }
            foreach (var child in scene.Children)
            {
                SearchUIElementInScene(child, elementName, ref foundElement, ref foundComponent);
                if (foundElement != null) return;
            }
        }

        private static void SearchUIElementInEntity(Entity entity, string elementName, ref UIElement foundElement, ref UIComponent foundComponent)
        {
            var uiComponent = entity.Get<UIComponent>();
            if (uiComponent?.Page?.RootElement != null)
            {
                var element = uiComponent.Page.RootElement.FindName(elementName);
                if (element != null)
                {
                    foundElement = element;
                    foundComponent = uiComponent;
                    return;
                }
            }

            if (entity.Transform != null)
            {
                foreach (var childTransform in entity.Transform.Children)
                {
                    if (childTransform.Entity != null)
                    {
                        SearchUIElementInEntity(childTransform.Entity, elementName, ref foundElement, ref foundComponent);
                        if (foundElement != null) return;
                    }
                }
            }
        }

        private static object FocusEntity(Game game, GameBridge bridge, string entityId, string entityName)
        {
            try
            {
                if (string.IsNullOrEmpty(entityId) && string.IsNullOrEmpty(entityName))
                    return new { error = "Either entityId or entityName is required for entity targeting" };

                var rootScene = game.SceneSystem?.SceneInstance?.RootScene;
                if (rootScene == null)
                    return new { error = "No scene loaded" };

                Entity found = null;
                if (!string.IsNullOrEmpty(entityId) && Guid.TryParse(entityId, out var guid))
                {
                    found = GetEntityTool.FindEntityById(rootScene, guid);
                }
                else if (!string.IsNullOrEmpty(entityName))
                {
                    found = GetEntityTool.FindEntityByName(rootScene, entityName);
                }

                if (found == null)
                    return new { error = $"Entity not found: {entityId ?? entityName}" };

                // Get entity world position
                var worldPos = found.Transform.WorldMatrix.TranslationVector;

                // Find the active camera (must be enabled)
                var camera = FindActiveCamera(rootScene);
                if (camera == null)
                    return new { error = "No enabled camera found in scene" };

                // Project world position to screen space
                var backBuffer = game.GraphicsDevice?.Presenter?.BackBuffer;
                if (backBuffer == null)
                    return new { error = "Back buffer not available" };

                var viewport = new Viewport(0, 0, backBuffer.Width, backBuffer.Height);

                // Update camera matrices with the correct screen aspect ratio
                var screenAspectRatio = (float)backBuffer.Width / backBuffer.Height;
                camera.Update(screenAspectRatio);
                var viewMatrix = camera.ViewMatrix;
                var projectionMatrix = camera.ProjectionMatrix;

                var screenPos = viewport.Project(worldPos, projectionMatrix, viewMatrix, Matrix.Identity);

                // Check for invalid projection results (NaN/Infinity)
                if (float.IsNaN(screenPos.X) || float.IsInfinity(screenPos.X) ||
                    float.IsNaN(screenPos.Y) || float.IsInfinity(screenPos.Y))
                {
                    return new { error = "Could not project entity position to screen (invalid camera matrices)" };
                }

                // Check depth — Z outside [0,1] means the entity is behind the camera or beyond the far plane
                if (screenPos.Z < 0 || screenPos.Z > 1)
                {
                    return new { error = $"Entity is behind the camera or outside the view frustum (depth: {screenPos.Z:F3})" };
                }

                // Normalize to 0-1
                var normalizedX = screenPos.X / backBuffer.Width;
                var normalizedY = screenPos.Y / backBuffer.Height;

                // Check if on screen
                if (normalizedX < 0 || normalizedX > 1 || normalizedY < 0 || normalizedY > 1)
                {
                    return new { error = $"Entity is off-screen (normalized: {normalizedX:F3}, {normalizedY:F3})" };
                }

                // Move the simulated mouse
                bridge.Mouse.SetPosition(new Vector2(normalizedX, normalizedY));

                return new
                {
                    success = true,
                    screenX = screenPos.X,
                    screenY = screenPos.Y,
                    normalizedX,
                    normalizedY,
                };
            }
            catch (Exception ex)
            {
                return new { error = $"Failed to focus entity: {ex.Message}" };
            }
        }

        private static CameraComponent FindActiveCamera(Scene scene)
        {
            foreach (var entity in scene.Entities)
            {
                var camera = FindCameraInEntity(entity);
                if (camera != null)
                    return camera;
            }
            foreach (var child in scene.Children)
            {
                var camera = FindActiveCamera(child);
                if (camera != null)
                    return camera;
            }
            return null;
        }

        private static CameraComponent FindCameraInEntity(Entity entity)
        {
            var camera = entity.Get<CameraComponent>();
            if (camera != null && camera.Enabled)
                return camera;

            if (entity.Transform != null)
            {
                foreach (var childTransform in entity.Transform.Children)
                {
                    if (childTransform.Entity != null)
                    {
                        var found = FindCameraInEntity(childTransform.Entity);
                        if (found != null)
                            return found;
                    }
                }
            }
            return null;
        }
    }
}
