// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics; // For Texture
using Stride.UI;
using Stride.UI.Controls; // For Image
using Stride.UI.Panels;   // For Canvas

namespace FirstPersonShooter.UI.Scripts
{
    public class MinimapPanelScript : SyncScript
    {
        // Public Properties
        public Texture MapTexture { get; set; }
        public Entity PlayerEntity { get; set; } // For actual player later
        public Vector2 MapTextureWorldSize { get; set; } = new Vector2(1000, 1000); // World units covered by map
        public Vector2 MapTexturePixelSize { get; set; } = new Vector2(1024, 1024); // Actual pixel dimensions of MapTexture
        public Vector2 WorldOriginOffset { get; set; } = Vector2.Zero; // Offset if (0,0) world != (0,0) map corner

        // UI Element References
        private UIElement minimapRootPanel; // The clipping container (MinimapRoot)
        private Image mapTextureImageElement;
        private Image playerMapIconElement; // Though its position is static, might need reference for other things

        // Private Fields
        private Vector2 mapPixelToWorldRatio;
        private Vector2 halfMinimapPanelSize;
        private bool isInitialized = false;

        // POI Fields
        public List<World.POIData> PointsOfInterest { get; set; } = new List<World.POIData>();
        private List<ImageElement> poiIconsOnMinimap = new List<ImageElement>();
        public Prefab POIMinimapIconPrefab { get; set; } // Assign in editor: simple Entity with UIComponent(ImageElement)

        public override void Start()
        {
            var rootElement = Entity.Get<UIComponent>()?.Page?.RootElement;
            if (rootElement == null)
            {
                Log.Error("MinimapPanelScript: Root UI element not found on this entity.");
                return;
            }

            minimapRootPanel = rootElement.FindName<UIElement>("MinimapRoot"); // This should be the Canvas itself from XAML
            mapTextureImageElement = rootElement.FindName<Image>("MapTextureImage");
            playerMapIconElement = rootElement.FindName<Image>("PlayerMapIcon");

            if (minimapRootPanel == null) Log.Error("MinimapPanelScript: MinimapRoot (Canvas) not found in UI.");
            if (mapTextureImageElement == null) Log.Error("MinimapPanelScript: MapTextureImage not found in UI.");
            if (playerMapIconElement == null) Log.Error("MinimapPanelScript: PlayerMapIcon not found in UI.");

            if (minimapRootPanel == null || mapTextureImageElement == null)
            {
                Log.Error("MinimapPanelScript: Critical UI elements missing, script will not initialize.");
                return;
            }

            if (MapTexture != null)
            {
                mapTextureImageElement.Source = new SpriteFromTexture(MapTexture);
                mapTextureImageElement.Width = MapTexturePixelSize.X;
                mapTextureImageElement.Height = MapTexturePixelSize.Y;
            }
            else
            {
                Log.Error("MinimapPanelScript: MapTexture is not assigned. Minimap will not display map.");
                // Optionally hide mapTextureImageElement or show a placeholder
                mapTextureImageElement.Visibility = Visibility.Collapsed;
                return; // Cannot proceed without map texture dimensions for calculations
            }
            
            if (MapTextureWorldSize.X == 0 || MapTextureWorldSize.Y == 0)
            {
                Log.Error("MinimapPanelScript: MapTextureWorldSize X or Y is zero, division by zero avoided. Set valid world size.");
                return;
            }
            mapPixelToWorldRatio = MapTexturePixelSize / MapTextureWorldSize;

            // ActualWidth/Height might not be immediately available in Start if layout pass hasn't run.
            // Consider moving this to first Update or a LayoutUpdated event if values are zero.
            // For fixed size UI, it's usually fine.
            if (minimapRootPanel.ActualWidth == 0 || minimapRootPanel.ActualHeight == 0)
            {
                 Log.Warning("MinimapPanelScript: minimapRootPanel ActualWidth/Height is 0 in Start. Using DesiredDesize. Calculations might be off until first layout pass.");
                 // Using DesiredSize as a fallback, assuming Width/Height were set in XAML
                 halfMinimapPanelSize = new Vector2(minimapRootPanel.DesiredSize.X / 2, minimapRootPanel.DesiredSize.Y / 2);

            } else {
                 halfMinimapPanelSize = new Vector2(minimapRootPanel.ActualWidth / 2, minimapRootPanel.ActualHeight / 2);
            }

            if (halfMinimapPanelSize.X == 0 || halfMinimapPanelSize.Y == 0)
            {
                Log.Error("MinimapPanelScript: halfMinimapPanelSize is zero. Ensure MinimapRoot has a defined size.");
                // Attempt to use XAML defined sizes if ActualSize is not available yet
                if (minimapRootPanel is FrameworkElement fe)
                {
                    halfMinimapPanelSize = new Vector2(fe.Width / 2, fe.Height / 2);
                    if (halfMinimapPanelSize.X == 0 || halfMinimapPanelSize.Y == 0)
                    {
                        Log.Error("MinimapPanelScript: Fallback to FrameworkElement Width/Height also resulted in zero size. Script may not work.");
                        return;
                    }
                     Log.Warning($"MinimapPanelScript: Used FrameworkElement Width/Height for panel size. ActualSize was 0. Panel size: {fe.Width}x{fe.Height}");
                } else {
                    return; // Cannot determine panel size
                }
            }


            // Player icon should be centered by its XAML properties (HorizontalAlignment/VerticalAlignment="Center")
            // If it needed specific Canvas.Left/Top:
            // playerMapIconElement?.SetCanvasPosition(new Vector2(halfMinimapPanelSize.X - (playerMapIconElement.ActualWidth / 2), 
            //                                                   halfMinimapPanelSize.Y - (playerMapIconElement.ActualHeight / 2)));
            
            isInitialized = true;
            LoadMockPOIs(); // Load POIs
            RefreshPOIIconsOnMinimap(); // Create UI for POIs
            Log.Info("MinimapPanelScript started and initialized.");
        }

        private void LoadMockPOIs()
        {
            PointsOfInterest.Clear();
            // Example POIs - positions should be within your MapTextureWorldSize
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
                WorldPosition = new Vector3(0,0,0) + new Vector3(WorldOriginOffset.X, 0, WorldOriginOffset.Y), // Example at origin
                IconTexture = null, IsDiscovered = true
            });
            Log.Info($"Loaded {PointsOfInterest.Count} mock POIs for minimap.");
        }
        
        private void RefreshPOIIconsOnMinimap()
        {
            if (minimapRootPanel == null || POIMinimapIconPrefab == null)
            {
                Log.Error("MinimapPanelScript: Cannot refresh POI icons. minimapRootPanel or POIMinimapIconPrefab is null.");
                return;
            }

            // Clear existing icons
            foreach (var icon in poiIconsOnMinimap)
            {
                // If icon is the RootElement of an Entity's UIComponent.Page, that entity should be removed from scene.
                // And icon itself (UIElement) removed from minimapRootPanel.Children.
                var parentEntity = icon.Entity; // This assumes icon is RootElement, so Entity is its host
                icon.Parent?.Children.Remove(icon); // Remove from UI parent
                parentEntity?.Scene?.Entities.Remove(parentEntity); // Remove entity from scene
            }
            poiIconsOnMinimap.Clear();

            foreach (var poi in PointsOfInterest)
            {
                if (!poi.IsDiscovered) continue;

                var poiIconEntityResult = POIMinimapIconPrefab.Instantiate();
                if (poiIconEntityResult == null || !poiIconEntityResult.Any())
                {
                    Log.Error($"MinimapPanelScript: Failed to instantiate POIMinimapIconPrefab for POI '{poi.Name}'.");
                    continue;
                }
                var poiIconEntity = poiIconEntityResult.First();
                
                // Ensure entity is in the scene for its UIComponent to be processed
                if (poiIconEntity.Scene == null) this.Entity.Scene?.Entities.Add(poiIconEntity);

                var uiComponent = poiIconEntity.Get<UIComponent>();
                var imageElement = uiComponent?.Page?.RootElement as Image; // Assuming prefab root is an Image

                if (imageElement == null)
                {
                    Log.Error($"MinimapPanelScript: POIMinimapIconPrefab for POI '{poi.Name}' does not have an ImageElement as its UI Page's RootElement or is missing UIComponent/Page.");
                    this.Entity.Scene?.Entities.Remove(poiIconEntity); // Cleanup
                    continue;
                }

                if (poi.IconTexture != null)
                {
                    imageElement.Source = new SpriteFromTexture(poi.IconTexture);
                }
                else
                {
                    // Default placeholder if no icon texture (e.g., a colored square)
                    imageElement.BackgroundColor = GetColorForPOIType(poi.Type); 
                    // Ensure prefab image size, e.g. 12x12. It's set in the XAML of POIMinimapIconPrefab.
                }
                
                // Add to minimap panel (visual hierarchy)
                minimapRootPanel.Children.Add(imageElement); 
                poiIconsOnMinimap.Add(imageElement);
            }
            Log.Info($"Refreshed {poiIconsOnMinimap.Count} POI icons on minimap.");
        }

        private Color GetColorForPOIType(World.POIType type)
        {
            switch (type)
            {
                case World.POIType.Landmark: return Colors.BlueViolet;
                case World.POIType.ResourceNode: return Colors.DarkGoldenrod;
                case World.POIType.Quest: return Colors.LawnGreen;
                case World.POIType.PlayerBase: return Colors.DodgerBlue;
                case World.POIType.CustomMarker: return Colors.HotPink;
                default: return Colors.WhiteSmoke;
            }
        }


        public override void Update()
        {
            if (!isInitialized || mapTextureImageElement == null || MapTexture == null)
            {
                if (isInitialized && (halfMinimapPanelSize.X == 0 || halfMinimapPanelSize.Y == 0) && minimapRootPanel.ActualWidth > 0) {
                     halfMinimapPanelSize = new Vector2(minimapRootPanel.ActualWidth / 2, minimapRootPanel.ActualHeight / 2);
                     Log.Info($"MinimapPanelScript: Updated halfMinimapPanelSize in Update to {halfMinimapPanelSize.X}, {halfMinimapPanelSize.Y}");
                } else if (!isInitialized) {
                    return;
                }
            }

            Vector3 playerWorldPos;
            if (PlayerEntity != null)
            {
                playerWorldPos = PlayerEntity.Transform.Position;
            }
            else
            {
                float time = (float)Game.UpdateTime.Total.TotalSeconds;
                playerWorldPos = new Vector3(time * 10f - 500f, 0, time * 8f - 400f); 
            }
            
            float playerMapX_pixels = (playerWorldPos.X - WorldOriginOffset.X) * mapPixelToWorldRatio.X;
            float playerMapY_pixels = (playerWorldPos.Z - WorldOriginOffset.Y) * mapPixelToWorldRatio.Y;
            
            float mapImageLeft = halfMinimapPanelSize.X - playerMapX_pixels;
            float mapImageTop = halfMinimapPanelSize.Y - playerMapY_pixels;

            mapTextureImageElement.SetCanvasPosition(new Vector2(mapImageLeft, mapImageTop));

            // Update POI Icon Positions
            for (int i = 0; i < poiIconsOnMinimap.Count; i++)
            {
                var icon = poiIconsOnMinimap[i];
                if (i >= PointsOfInterest.Count) break; // Should not happen if lists are synced
                var poi = PointsOfInterest[i];

                if (!poi.IsDiscovered) {
                    icon.Visibility = Visibility.Collapsed;
                    continue;
                }
                
                float poiMapX_pixels = (poi.WorldPosition.X - WorldOriginOffset.X) * mapPixelToWorldRatio.X;
                float poiMapY_pixels = (poi.WorldPosition.Z - WorldOriginOffset.Y) * mapPixelToWorldRatio.Y;

                // Position relative to the panned map image's top-left corner, then adjust for icon center
                float iconPosX = mapImageLeft + poiMapX_pixels - (icon.ActualWidth > 0 ? icon.ActualWidth / 2 : icon.Width / 2); // Use XAML Width if ActualWidth not ready
                float iconPosY = mapImageTop + poiMapY_pixels - (icon.ActualHeight > 0 ? icon.ActualHeight / 2 : icon.Height / 2);

                icon.SetCanvasPosition(new Vector2(iconPosX, iconPosY));

                // Visibility Check (simple - check if icon center is roughly within panel bounds)
                // A more accurate check would use icon.ActualWidth/Height and compare full bounds.
                bool isVisible = iconPosX + (icon.ActualWidth > 0 ? icon.ActualWidth : icon.Width) > 0 && iconPosX < minimapRootPanel.ActualWidth &&
                                 iconPosY + (icon.ActualHeight > 0 ? icon.ActualHeight : icon.Height) > 0 && iconPosY < minimapRootPanel.ActualHeight;
                icon.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
            }

            // Player Icon Rotation Logic
            if (PlayerEntity != null && playerMapIconElement != null)
            {
                Quaternion playerWorldRot = PlayerEntity.Transform.Rotation;
                // Convert quaternion to Euler angles (pitch, yaw, roll)
                playerWorldRot.ToEulerAngles(out Vector3 eulerAngles);
                float playerYawRadians = eulerAngles.Y; // Yaw is around the Y-axis

                // UI elements rotate in degrees. Positive angle usually means clockwise.
                // Player yaw: positive for right turn (clockwise looking from above).
                // If icon's "forward" is "up" (0 degrees on UI), then world yaw needs to be applied.
                // The sign depends on coordinate systems: if UI rotation is CW positive, and world yaw is CW positive,
                // it should be direct. Often, one is inverted relative to the other.
                // A common case: world yaw increases clockwise, UI angle increases clockwise.
                // Player forward (world +Z) might correspond to icon up (UI -Y).
                // Stride UI rotation is clockwise positive. Player entity yaw (Y-axis rotation) is typically
                // counter-clockwise positive if using standard right-hand rule for rotations around Y.
                // So, a negation might be needed. Let's test with direct mapping first, then adjust.
                // Stride's Euler angles: Yaw is rotation about Y. Pitch about X. Roll about Z.
                // A positive yaw in Stride means turning left (counter-clockwise when viewed from above).
                // UI rotation often has positive as clockwise. So, angle = -degrees(yaw).

                float rotationInDegrees = -MathUtil.RadiansToDegrees(playerYawRadians);

                var rotateTransform = playerMapIconElement.RenderTransform as RotateTransform;
                if (rotateTransform == null)
                {
                    rotateTransform = new RotateTransform();
                    playerMapIconElement.RenderTransform = rotateTransform;
                    // Ensure rotation happens around the center of the icon
                    playerMapIconElement.RenderTransformOrigin = new Vector2(0.5f, 0.5f);
                }
                rotateTransform.Angle = rotationInDegrees;
            }
            else if (playerMapIconElement != null) // No PlayerEntity, reset icon rotation
            {
                var rotateTransform = playerMapIconElement.RenderTransform as RotateTransform;
                if (rotateTransform != null && rotateTransform.Angle != 0)
                {
                    rotateTransform.Angle = 0;
                }
            }
        }
    }
}
