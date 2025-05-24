// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics; // For Texture
using Stride.UI;
using Stride.UI.Controls; // For Image, Button
using Stride.UI.Panels;   // For Canvas
using Stride.UI.Events;   // For RoutedEventArgs

namespace FirstPersonShooter.UI.Scripts
{
    public class FullMapPanelScript : SyncScript
    {
        // Public Properties
        public Texture MapTexture { get; set; }
        public Entity PlayerEntity { get; set; } // For actual player later
        public Vector2 MapTextureWorldSize { get; set; } = new Vector2(1000, 1000); // World units covered by map
        public Vector2 MapTexturePixelSize { get; set; } = new Vector2(1024, 1024); // Actual pixel dimensions of MapTexture
        public Vector2 WorldOriginOffset { get; set; } = Vector2.Zero; // Offset if (0,0) world != (0,0) map corner

        // UI Element References
        private Image fullMapTextureImageElement;
        private Image fullMapPlayerIconElement;
        private Button closeMapButton;

        // Private Fields
        private Vector2 mapPixelToWorldRatio;
        private bool isInitialized = false;
        private UIElement rootElement; // This is FullMapRoot (Canvas)

        // New Fields for Zoom/Pan State
        private float currentZoom = 1.0f;
        private const float minZoom = 0.5f;
        private const float maxZoom = 3.0f;
        private const float zoomSpeed = 0.1f;
        private Vector2 mapPanOffset = Vector2.Zero; // Pan offset in screen pixels, relative to centered position
        private bool isPanning = false;
        private Vector2 lastMousePositionForPan; // In screen (normalized or absolute) coordinates for delta calculation
        private Vector2 initialCenterOffset = Vector2.Zero; // Stores the initial offset to keep map centered before panning

        // POI Fields
        public List<World.POIData> PointsOfInterest { get; set; } = new List<World.POIData>();
        private List<ImageElement> poiIconsOnFullMap = new List<ImageElement>();
        public Prefab POIFullMapIconPrefab { get; set; } // Assign in editor, e.g. 20x20 Image

        public override void Start()
        {
            rootElement = Entity.Get<UIComponent>()?.Page?.RootElement;
            if (rootElement == null)
            {
                Log.Error("FullMapPanelScript: Root UI element (FullMapRoot) not found on this entity.");
                return;
            }

            fullMapTextureImageElement = rootElement.FindName<Image>("FullMapTextureImage");
            fullMapPlayerIconElement = rootElement.FindName<Image>("FullMapPlayerIcon");
            closeMapButton = rootElement.FindName<Button>("CloseMapButton");

            if (fullMapTextureImageElement == null) Log.Error("FullMapPanelScript: FullMapTextureImage not found in UI.");
            if (fullMapPlayerIconElement == null) Log.Error("FullMapPanelScript: FullMapPlayerIcon not found in UI.");
            if (closeMapButton == null) Log.Warning("FullMapPanelScript: CloseMapButton not found in UI (optional).");


            if (MapTexture != null)
            {
                fullMapTextureImageElement.Source = new SpriteFromTexture(MapTexture);
                fullMapTextureImageElement.Width = MapTexturePixelSize.X;
                fullMapTextureImageElement.Height = MapTexturePixelSize.Y;
            }
            else
            {
                Log.Error("FullMapPanelScript: MapTexture is not assigned. Full map will not display map.");
                if (fullMapTextureImageElement != null) fullMapTextureImageElement.Visibility = Visibility.Collapsed;
                // Do not return yet, as other parts like close button might still need to function or player icon for a blank map.
            }
            
            if (MapTextureWorldSize.X == 0 || MapTextureWorldSize.Y == 0 || MapTexturePixelSize.X == 0 || MapTexturePixelSize.Y == 0)
            {
                Log.Error("FullMapPanelScript: MapTextureWorldSize or MapTexturePixelSize has zero components. Division by zero avoided. Set valid sizes.");
                isInitialized = false; // Prevent update logic from running with bad ratios
                return;
            }
            mapPixelToWorldRatio = MapTexturePixelSize / MapTextureWorldSize;

            if (closeMapButton != null)
            {
                closeMapButton.Click += CloseMapButton_Click;
            }

            // Initial centering of the map image. ActualWidth/Height might be zero in Start.
            // Deferring to Update or a LayoutUpdated event is more robust for dynamic sizing.
            // For now, assume rootElement has its size from XAML (e.g., 90% of screen).
            // The image's Canvas.Left/Top will be set to center it within rootElement.
            // This initial centering logic might be better in the first Update tick where ActualWidth/Height are reliable.
            CalculateInitialCenterOffset(); // Sets initialCenterOffset
            mapPanOffset = initialCenterOffset; // Start with map centered

            if (fullMapTextureImageElement != null)
            {
                fullMapTextureImageElement.RenderTransform = new ScaleTransform(currentZoom, currentZoom);
                fullMapTextureImageElement.RenderTransformOrigin = new Vector2(0.5f, 0.5f); // Scale from center
            }
            
            ApplyMapTransformations(); // Apply initial pan (centering) and scale

            isInitialized = true;
            LoadMockPOIs_FullMap(); // Load POIs for the full map
            RefreshPOIIconsOnFullMap(); // Create UI for POIs on full map
            Log.Info("FullMapPanelScript started and initialized.");
        }
        
        // This method can be shared or distinct from Minimap's version
        private void LoadMockPOIs_FullMap() 
        {
            PointsOfInterest.Clear();
            // Re-using the same mock POIs as minimap for consistency in this example
             PointsOfInterest.Add(new World.POIData 
            { 
                ID = "Cave1", Name = "Cave Entrance", Type = World.POIType.Landmark, 
                WorldPosition = new Vector3(100, 0, 200) + new Vector3(WorldOriginOffset.X, 0, WorldOriginOffset.Y), 
                IconTexture = null, IsDiscovered = true 
            });
            PointsOfInterest.Add(new World.POIData 
            { 
                ID = "IronNode1", Name = "Iron Deposit", Type = World.POIType.ResourceNode, 
                WorldPosition = new Vector3(-150, 0, -100) + new Vector3(WorldOriginOffset.X, 0, WorldOriginOffset.Y), 
                IconTexture = null, IsDiscovered = true 
            });
            PointsOfInterest.Add(new World.POIData
            {
                ID = "PlayerBase1", Name = "My Base", Type = World.POIType.PlayerBase,
                WorldPosition = new Vector3(0,0,0) + new Vector3(WorldOriginOffset.X, 0, WorldOriginOffset.Y),
                IconTexture = null, IsDiscovered = true
            });
            Log.Info($"Loaded {PointsOfInterest.Count} mock POIs for full map.");
        }

        private void RefreshPOIIconsOnFullMap()
        {
            if (rootElement == null || POIFullMapIconPrefab == null) // rootElement is FullMapRoot (Canvas)
            {
                Log.Error("FullMapPanelScript: Cannot refresh POI icons. FullMapRoot or POIFullMapIconPrefab is null.");
                return;
            }

            foreach (var icon in poiIconsOnFullMap)
            {
                var parentEntity = icon.Entity;
                icon.Parent?.Children.Remove(icon);
                parentEntity?.Scene?.Entities.Remove(parentEntity);
            }
            poiIconsOnFullMap.Clear();

            foreach (var poi in PointsOfInterest)
            {
                if (!poi.IsDiscovered) continue;

                var poiIconEntityResult = POIFullMapIconPrefab.Instantiate();
                if (poiIconEntityResult == null || !poiIconEntityResult.Any()) { /* Log error */ continue; }
                var poiIconEntity = poiIconEntityResult.First();
                
                if (poiIconEntity.Scene == null) this.Entity.Scene?.Entities.Add(poiIconEntity);

                var uiComponent = poiIconEntity.Get<UIComponent>();
                var imageElement = uiComponent?.Page?.RootElement as Image;

                if (imageElement == null) { /* Log error, cleanup entity */ continue; }

                if (poi.IconTexture != null)
                {
                    imageElement.Source = new SpriteFromTexture(poi.IconTexture);
                }
                else
                {
                    imageElement.BackgroundColor = GetColorForPOIType_FullMap(poi.Type);
                    // Ensure prefab image has a size, e.g., 20x20 in its XAML.
                }
                
                // Add to FullMapRoot (Canvas)
                rootElement.Children.Add(imageElement); 
                poiIconsOnFullMap.Add(imageElement);
            }
            Log.Info($"Refreshed {poiIconsOnFullMap.Count} POI icons on full map.");
            ApplyMapTransformations(); // Re-apply transformations to position newly added POIs
        }
        
        // Helper to get color for POI type, can be shared or specific
        private Color GetColorForPOIType_FullMap(World.POIType type)
        {
            switch (type)
            {
                case World.POIType.Landmark: return Color.FromBgra(0xFF9370DB); // MediumPurple
                case World.POIType.ResourceNode: return Color.FromBgra(0xFFD2B48C); // Tan
                case World.POIType.Quest: return Color.FromBgra(0xFF7FFF00); // Chartreuse
                case World.POIType.PlayerBase: return Color.FromBgra(0xFF1E90FF); // DodgerBlue
                case World.POIType.CustomMarker: return Color.FromBgra(0xFFFF69B4); // HotPink
                default: return Color.FromBgra(0xFFF5F5F5); // WhiteSmoke
            }
        }


        private void CalculateInitialCenterOffset()
        {
            if (rootElement == null || fullMapTextureImageElement == null) 
            {
                initialCenterOffset = Vector2.Zero;
                return;
            }

            float panelWidth = rootElement.ActualWidth;
            float panelHeight = rootElement.ActualHeight;
            if (panelWidth == 0 && rootElement is FrameworkElement fePanel) panelWidth = fePanel.Width;
            if (panelHeight == 0 && rootElement is FrameworkElement fePanelH) panelHeight = fePanelH.Height;

            // Assuming MapTexturePixelSize is the unscaled image size
            float imageWidth = MapTexturePixelSize.X; 
            float imageHeight = MapTexturePixelSize.Y;

            if (panelWidth > 0 && panelHeight > 0 && imageWidth > 0 && imageHeight > 0)
            {
                 initialCenterOffset = new Vector2((panelWidth - imageWidth * currentZoom) / 2, 
                                                   (panelHeight - imageHeight * currentZoom) / 2);
            } else {
                Log.Warning("FullMapPanelScript: Could not calculate initial center offset in Start due to zero dimensions for panel or image. Will default to (0,0).");
                initialCenterOffset = Vector2.Zero;
            }
        }


        public override void Update()
        {
            if (!isInitialized || rootElement.Visibility == Visibility.Collapsed)
            {
                return;
            }
            
            bool transformationsChanged = false;

            // Zoom Logic
            float mouseWheelDelta = Input.MouseWheelDelta;
            if (mouseWheelDelta != 0 && fullMapTextureImageElement != null)
            {
                Vector2 mousePosInUIToRoot = rootElement.ScreenToLocal(Input.MousePosition); // Mouse pos relative to FullMapRoot

                float oldZoom = currentZoom;
                currentZoom += mouseWheelDelta * zoomSpeed;
                currentZoom = MathUtil.Clamp(currentZoom, minZoom, maxZoom);

                // Effective size of the map image
                float mapDisplayWidth = MapTexturePixelSize.X * oldZoom;
                float mapDisplayHeight = MapTexturePixelSize.Y * oldZoom;
                
                // Point on the map image that was under the mouse (0,0 is top-left of map image, before scaling from center)
                // mapPanOffset is the Canvas.Left/Top of the map image.
                // RenderTransformOrigin (0.5,0.5) means scaling happens around map's center.
                // The visual top-left of the map image relative to its center is (-mapDisplayWidth/2, -mapDisplayHeight/2).
                // So, mouse relative to map's visual top-left:
                Vector2 mouseRelativeToMapVisualTopLeft = mousePosInUIToRoot - mapPanOffset - new Vector2(-mapDisplayWidth/2, -mapDisplayHeight/2);

                // Convert this to a normalized pivot point (0-1 range) on the map image
                Vector2 pivotNormalized = mouseRelativeToMapVisualTopLeft / new Vector2(mapDisplayWidth, mapDisplayHeight);
                
                // New display size
                float newMapDisplayWidth = MapTexturePixelSize.X * currentZoom;
                float newMapDisplayHeight = MapTexturePixelSize.Y * currentZoom;

                // The new pan offset should keep the pivotNormalized point under mousePosInUIToRoot
                // mapPanOffset + (-newMapDisplayWidth/2) + pivotNormalized * newMapDisplayWidth = mousePosInUIToRoot
                mapPanOffset = mousePosInUIToRoot - (new Vector2(-newMapDisplayWidth/2, -newMapDisplayHeight/2) + pivotNormalized * new Vector2(newMapDisplayWidth, newMapDisplayHeight));
                
                transformationsChanged = true;
            }

            // Pan Logic
            if (Input.IsMouseButtonPressed(MouseButton.Left)) // Or MiddleButton
            {
                // Check if mouse is over the map panel to initiate panning
                if (rootElement.IsPointInside(Input.MousePosition)) // IsPointInside expects normalized screen coords
                {
                    isPanning = true;
                    lastMousePositionForPan = Input.MousePosition; // Store initial screen mouse position
                }
            }
            if (Input.IsMouseButtonDown(MouseButton.Left) && isPanning)
            {
                Vector2 currentScreenMousePos = Input.MousePosition;
                Vector2 screenDelta = currentScreenMousePos - lastMousePositionForPan;
                
                // Convert screenDelta to UI local delta (pixels)
                // This requires knowing the scale factor if UI itself is scaled, but usually not needed if rootElement is full-screen or fixed size.
                // For direct pixel delta, we need to consider the UI scaling if any.
                // Assuming 1:1 for now, or that rootElement.ScreenToLocal handles this implicitly for delta.
                // A simpler approach: mouseDelta is already in the "screen space" that UI positions use.
                // However, Input.MousePosition is normalized (0-1). We need pixel delta.
                // This requires knowing the actual screen resolution or UI root size.
                // Let's assume screenDelta needs to be scaled by UI resolution.
                // Stride UI positions are in virtual pixels. Input.MousePosition is normalized.
                // So, delta in virtual pixels = screenDelta * new Vector2(GraphicsDevice.Presenter.BackBuffer.Width, GraphicsDevice.Presenter.BackBuffer.Height);
                // For simplicity, assuming screenDelta can be directly applied if UI is not globally scaled.
                // Let's use the change in local coordinates to be safer if UI root has transformations.
                Vector2 localMouseNow = rootElement.ScreenToLocal(currentScreenMousePos);
                Vector2 localMousePrev = rootElement.ScreenToLocal(lastMousePositionForPan);
                Vector2 localDelta = localMouseNow - localMousePrev;

                mapPanOffset += localDelta;
                lastMousePositionForPan = currentScreenMousePos;
                transformationsChanged = true;
            }
            if (Input.IsMouseButtonReleased(MouseButton.Left))
            {
                isPanning = false;
            }

            if (transformationsChanged || PlayerEntity != null) // Update if pan/zoom or if player exists (for movement)
            {
                ApplyMapTransformations();
            }
        }

        private void ApplyMapTransformations()
        {
            if (fullMapTextureImageElement == null) return;

            // Apply Scale
            var scaleTransform = fullMapTextureImageElement.RenderTransform as ScaleTransform;
            if (scaleTransform == null) {
                scaleTransform = new ScaleTransform();
                fullMapTextureImageElement.RenderTransform = scaleTransform;
                fullMapTextureImageElement.RenderTransformOrigin = new Vector2(0.5f, 0.5f);
            }
            scaleTransform.Scale = new Vector2(currentZoom, currentZoom);
            
            // Apply Pan
            fullMapTextureImageElement.SetCanvasPosition(mapPanOffset);

            // --- Update Player Icon ---
            if (PlayerEntity != null && fullMapPlayerIconElement != null)
            {
                if (fullMapPlayerIconElement.Visibility == Visibility.Collapsed) fullMapPlayerIconElement.Visibility = Visibility.Visible;

                Vector3 playerWorldPos = PlayerEntity.Transform.Position;
                Quaternion playerWorldRot = PlayerEntity.Transform.Rotation;

                float mapX_unzoomed_pixels = (playerWorldPos.X - WorldOriginOffset.X) * mapPixelToWorldRatio.X;
                float mapY_unzoomed_pixels = (playerWorldPos.Z - WorldOriginOffset.Y) * mapPixelToWorldRatio.Y;
                
                float playerOffsetXFromMapCenter_unzoomed = mapX_unzoomed_pixels - MapTexturePixelSize.X * 0.5f;
                float playerOffsetYFromMapCenter_unzoomed = mapY_unzoomed_pixels - MapTexturePixelSize.Y * 0.5f;
                float playerOffsetXFromMapCenter_scaled = playerOffsetXFromMapCenter_unzoomed * currentZoom;
                float playerOffsetYFromMapCenter_scaled = playerOffsetYFromMapCenter_unzoomed * currentZoom;
                Vector2 mapVisualCenterOnCanvas = mapPanOffset + (MapTexturePixelSize * currentZoom * 0.5f);
                float iconCenterXOnCanvas = mapVisualCenterOnCanvas.X + playerOffsetXFromMapCenter_scaled;
                float iconCenterYOnCanvas = mapVisualCenterOnCanvas.Y + playerOffsetYFromMapCenter_scaled;
                float iconActualWidth = fullMapPlayerIconElement.ActualWidth > 0 ? fullMapPlayerIconElement.ActualWidth : fullMapPlayerIconElement.Width;
                float iconActualHeight = fullMapPlayerIconElement.ActualHeight > 0 ? fullMapPlayerIconElement.ActualHeight : fullMapPlayerIconElement.Height;
                float iconCanvasX = iconCenterXOnCanvas - (iconActualWidth / 2);
                float iconCanvasY = iconCenterYOnCanvas - (iconActualHeight / 2);
                fullMapPlayerIconElement.SetCanvasPosition(new Vector2(iconCanvasX, iconCanvasY));

                playerWorldRot.ToEulerAngles(out Vector3 eulerAngles);
                float playerYawRadians = eulerAngles.Y;
                float rotationInDegrees = -MathUtil.RadiansToDegrees(playerYawRadians);
                var iconRotateTransform = fullMapPlayerIconElement.RenderTransform as RotateTransform;
                if (iconRotateTransform == null)
                {
                    iconRotateTransform = new RotateTransform();
                    fullMapPlayerIconElement.RenderTransform = iconRotateTransform;
                }
                iconRotateTransform.Angle = rotationInDegrees;
            }
            else if (fullMapPlayerIconElement != null)
            {
                fullMapPlayerIconElement.Visibility = Visibility.Collapsed;
            }

            // --- Update POI Icons ---
            for (int i = 0; i < poiIconsOnFullMap.Count; i++)
            {
                var icon = poiIconsOnFullMap[i];
                if (i >= PointsOfInterest.Count) break;
                var poi = PointsOfInterest[i];

                if (!poi.IsDiscovered) {
                    icon.Visibility = Visibility.Collapsed;
                    continue;
                }
                
                float poiMapX_unzoomed_pixels = (poi.WorldPosition.X - WorldOriginOffset.X) * mapPixelToWorldRatio.X;
                float poiMapY_unzoomed_pixels = (poi.WorldPosition.Z - WorldOriginOffset.Y) * mapPixelToWorldRatio.Y;

                // POI icon position relative to the scaled map image's center, then offset by mapPanOffset.
                float poiOffsetXFromMapCenter_unzoomed = poiMapX_unzoomed_pixels - MapTexturePixelSize.X * 0.5f;
                float poiOffsetYFromMapCenter_unzoomed = poiMapY_unzoomed_pixels - MapTexturePixelSize.Y * 0.5f;
                float poiOffsetXFromMapCenter_scaled = poiOffsetXFromMapCenter_unzoomed * currentZoom;
                float poiOffsetYFromMapCenter_scaled = poiOffsetYFromMapCenter_unzoomed * currentZoom;
                // mapVisualCenterOnCanvas is same as for player icon
                float poiIconCenterXOnCanvas = mapVisualCenterOnCanvas.X + poiOffsetXFromMapCenter_scaled;
                float poiIconCenterYOnCanvas = mapVisualCenterOnCanvas.Y + poiOffsetYFromMapCenter_scaled;
                
                float poiIconActualWidth = icon.ActualWidth > 0 ? icon.ActualWidth : icon.Width;
                float poiIconActualHeight = icon.ActualHeight > 0 ? icon.ActualHeight : icon.Height;

                float poiIconCanvasX = poiIconCenterXOnCanvas - (poiIconActualWidth / 2);
                float poiIconCanvasY = poiIconCenterYOnCanvas - (poiIconActualHeight / 2);

                icon.SetCanvasPosition(new Vector2(poiIconCanvasX, poiIconCanvasY));
                
                // Optional: Scale POI icons (e.g., to keep them fixed screen size or partially scale)
                // For now, they scale with the map as they are direct children of FullMapRoot Canvas
                // and we are not applying inverse scale to them. If they were children of fullMapTextureImageElement,
                // they would automatically scale. Since they are children of rootElement (Canvas), their size is fixed
                // unless we change their Width/Height or apply a RenderTransform.
                // The current logic means they are fixed size on the canvas, and their position is updated.
                // This is generally desirable for POI icons (fixed screen size).

                // Visibility Check (based on their final position on the Canvas)
                // Check if the center of the POI icon is within the visible bounds of the rootElement (FullMapRoot)
                bool isVisible = poiIconCenterXOnCanvas >= 0 && poiIconCenterXOnCanvas <= rootElement.ActualWidth &&
                                 poiIconCenterYOnCanvas >= 0 && poiIconCenterYOnCanvas <= rootElement.ActualHeight;
                icon.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            }
        }


        private void CloseMapButton_Click(object sender, RoutedEventArgs e)
        {
            Log.Info("FullMapPanelScript: Close button clicked.");
            if (rootElement != null)
            {
                rootElement.Visibility = Visibility.Collapsed;
                // Notify MainGameUIScript to handle mouse state
                // This requires a reference to MainGameUIScript or an event system.
                // For now, the MainGameUIScript's Escape key will handle this.
                // Or, if this panel is directly managed by MainGameUIScript's TogglePanelVisibility,
                // it might automatically reset mouse state when any panel is hidden.
                // Let's assume MainGameUIScript.TogglePanelVisibility(null) or similar is called by it.
                var mainUiEntity = Entity.Scene?.RootEntities.FirstOrDefault(ent => ent.Get<MainGameUIScript>() != null);
                mainUiEntity?.Get<MainGameUIScript>()?.CloseCurrentPanel(); // Assuming such a method exists
            }
        }
        
        public override void Cancel()
        {
            if (closeMapButton != null)
            {
                closeMapButton.Click -= CloseMapButton_Click;
            }
            base.Cancel();
        }
    }
}
