// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using Stride.Input; // Required for Input and Keys
using Stride.UI;
using System.Collections.Generic;
using System.Linq; // For FirstOrDefault

namespace FirstPersonShooter.UI.Scripts
{
    public class MainGameUIScript : SyncScript // SyncScript for Update method
    {
        public UIElement InventoryPanelHost { get; set; }
        public UIElement CraftingPanelHost { get; set; }
        public UIElement EngramPanelHost { get; set; }

        private List<UIElement> allPanels;
        private UIElement currentlyVisiblePanel = null;

        public override void Start()
        {
            var rootElement = Entity.Get<UIComponent>()?.Page?.RootElement;
            if (rootElement == null)
            {
                Log.Error("MainGameUIScript: Root UI element not found on this entity.");
                return;
            }

            InventoryPanelHost = rootElement.FindName<UIElement>("InventoryPanelHost");
            CraftingPanelHost = rootElement.FindName<UIElement>("CraftingPanelHost");
            EngramPanelHost = rootElement.FindName<UIElement>("EngramPanelHost");

            if (InventoryPanelHost == null) Log.Error("MainGameUIScript: InventoryPanelHost not found in UI.");
            if (CraftingPanelHost == null) Log.Error("MainGameUIScript: CraftingPanelHost not found in UI.");
            if (EngramPanelHost == null) Log.Error("MainGameUIScript: EngramPanelHost not found in UI.");
            
            allPanels = new List<UIElement>();
            if (InventoryPanelHost != null) allPanels.Add(InventoryPanelHost);
            if (CraftingPanelHost != null) allPanels.Add(CraftingPanelHost);
            if (EngramPanelHost != null) allPanels.Add(EngramPanelHost);

            // Ensure all panels are initially hidden
            foreach (var panel in allPanels)
            {
                panel.Visibility = Visibility.Collapsed;
            }
            
            // Set initial mouse state (assuming game starts with no UI panel open)
            SetMouseState(false); // Game mode: mouse locked and hidden

            Log.Info("MainGameUIScript started. Press I for Inventory, K for Crafting, N for Engrams. Press ESC to close any open panel.");
        }

        public override void Update()
        {
            if (Input.IsKeyPressed(Keys.I))
            {
                TogglePanelVisibility(InventoryPanelHost);
            }
            else if (Input.IsKeyPressed(Keys.K)) // Use else if to prevent multiple panels from trying to open on same frame
            {
                TogglePanelVisibility(CraftingPanelHost);
            }
            else if (Input.IsKeyPressed(Keys.N))
            {
                TogglePanelVisibility(EngramPanelHost);
            }
            else if (Input.IsKeyPressed(Keys.Escape))
            {
                if (currentlyVisiblePanel != null) // If a panel is open
                {
                    HideAllPanels();
                    SetMouseState(false); // Return to game mode
                }
            }
        }

        private void TogglePanelVisibility(UIElement panelToShow)
        {
            if (panelToShow == null) return;

            bool wasActive = panelToShow.Visibility == Visibility.Visible;

            // Hide all panels first (this will set currentlyVisiblePanel to null)
            HideAllPanels(); 

            if (!wasActive) // If it was hidden, and we want to show it
            {
                panelToShow.Visibility = Visibility.Visible;
                currentlyVisiblePanel = panelToShow;
                SetMouseState(true); // UI mode: mouse unlocked and visible
            }
            else // It was active, and HideAllPanels just hid it. So we want to return to game mode.
            {
                // currentlyVisiblePanel is already null from HideAllPanels
                SetMouseState(false); // Game mode: mouse locked and hidden
            }
        }

        private void HideAllPanels()
        {
            foreach (var panel in allPanels)
            {
                if (panel != null) panel.Visibility = Visibility.Collapsed;
            }
            currentlyVisiblePanel = null;
        }

        private void SetMouseState(bool uiMode)
        {
            if (uiMode)
            {
                Input.IsMousePositionLocked = false;
                Game.IsMouseVisible = true;
            }
            else
            {
                Input.IsMousePositionLocked = true;
                Game.IsMouseVisible = false;
            }
        }
    }
}
