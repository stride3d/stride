// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using Stride.Graphics; // For Texture
using Stride.UI;
using Stride.UI.Controls; // Image, TextBlock, Button, ScrollViewer
using Stride.UI.Panels;   // StackPanel
using Stride.UI.Events;   // RoutedEventArgs
using System.Collections.Generic;
using System.Linq; // For FirstOrDefault
using FirstPersonShooter.Items.Crafting; // For CraftingRecipe, RequiredResource

namespace FirstPersonShooter.UI.Scripts
{
    public class CraftingPanelScript : UIScript
    {
        // Public properties - assign in Stride Editor
        public Prefab RecipeListItemPrefab { get; set; }
        // Optional: public Prefab RequiredResourceDisplayPrefab { get; set; }

        // Private fields for UI elements
        private StackPanel recipeListPanel; // The StackPanel inside the ScrollViewer
        private ImageElement selectedItemIcon;
        private TextBlock selectedItemNameText;
        private TextBlock selectedItemDescriptionText;
        private StackPanel requiredResourcesPanel;
        private TextBlock craftingTimeText;
        private Button craftButton;
        
        private UIElement rootElement;

        private List<CraftingRecipe> availableRecipes = new List<CraftingRecipe>();
        private CraftingRecipe currentSelectedRecipe = null;

        public override void Start()
        {
            base.Start();

            rootElement = Entity.Get<UIComponent>()?.Page?.RootElement;
            if (rootElement == null)
            {
                Log.Error("CraftingPanelScript: Root UI element not found.");
                return;
            }

            // Find UI elements by name
            recipeListPanel = rootElement.FindName<StackPanel>("RecipeListPanel");
            selectedItemIcon = rootElement.FindName<ImageElement>("SelectedItemIcon");
            selectedItemNameText = rootElement.FindName<TextBlock>("SelectedItemNameText");
            selectedItemDescriptionText = rootElement.FindName<TextBlock>("SelectedItemDescriptionText");
            requiredResourcesPanel = rootElement.FindName<StackPanel>("RequiredResourcesPanel");
            craftingTimeText = rootElement.FindName<TextBlock>("CraftingTimeText");
            craftButton = rootElement.FindName<Button>("CraftButton");

            // Log errors if critical elements are not found
            if (recipeListPanel == null) Log.Error("CraftingPanelScript: RecipeListPanel not found.");
            if (selectedItemIcon == null) Log.Error("CraftingPanelScript: SelectedItemIcon not found.");
            if (selectedItemNameText == null) Log.Error("CraftingPanelScript: SelectedItemNameText not found.");
            if (selectedItemDescriptionText == null) Log.Error("CraftingPanelScript: SelectedItemDescriptionText not found.");
            if (requiredResourcesPanel == null) Log.Error("CraftingPanelScript: RequiredResourcesPanel not found.");
            if (craftingTimeText == null) Log.Error("CraftingPanelScript: CraftingTimeText not found.");
            if (craftButton == null) Log.Error("CraftingPanelScript: CraftButton not found.");

            if (RecipeListItemPrefab == null)
            {
                Log.Error("CraftingPanelScript: RecipeListItemPrefab is not assigned. Please assign it in the editor.");
                return; // Cannot proceed without this
            }

            LoadMockRecipes();
            PopulateRecipeList();

            if (craftButton != null)
            {
                craftButton.Click += HandleCraftButtonClick;
            }

            if (availableRecipes.Count > 0)
            {
                OnRecipeSelected(availableRecipes[0]);
            }
        }

        private void LoadMockRecipes()
        {
            availableRecipes.Clear();

            // Example 1: Wooden Plank
            var plankRecipe = new CraftingRecipe
            {
                RecipeID = "Plank",
                ItemIDToCraft = "WoodenPlank",
                DisplayName = "Wooden Plank",
                Description = "A simple wooden plank, useful for basic construction.",
                Icon = null, // No texture for now
                RequiredResources = new List<RequiredResource>
                {
                    new RequiredResource { ItemID = "WoodLog", DisplayName = "Wood Log", Quantity = 1, Icon = null }
                },
                CraftingTime = 0.5f,
                OutputQuantity = 2
            };
            availableRecipes.Add(plankRecipe);

            // Example 2: Stone Axe
            var axeRecipe = new CraftingRecipe
            {
                RecipeID = "StoneAxe",
                ItemIDToCraft = "StoneAxeItem",
                DisplayName = "Stone Axe",
                Description = "A crude axe made of stone and wood. Good for chopping trees.",
                Icon = null,
                RequiredResources = new List<RequiredResource>
                {
                    new RequiredResource { ItemID = "WoodenPlank", DisplayName = "Wooden Plank", Quantity = 2, Icon = null },
                    new RequiredResource { ItemID = "Stone", DisplayName = "Stone", Quantity = 3, Icon = null },
                    new RequiredResource { ItemID = "Fiber", DisplayName = "Fiber", Quantity = 5, Icon = null }
                },
                CraftingTime = 2.0f,
                OutputQuantity = 1
            };
            availableRecipes.Add(axeRecipe);

            // Example 3: Simple Bandage
            var bandageRecipe = new CraftingRecipe
            {
                RecipeID = "SimpleBandage",
                ItemIDToCraft = "BandageItem",
                DisplayName = "Simple Bandage",
                Description = "A piece of cloth used to stop bleeding and heal minor wounds.",
                Icon = null,
                RequiredResources = new List<RequiredResource>
                {
                    new RequiredResource { ItemID = "Cloth", DisplayName = "Cloth", Quantity = 2, Icon = null }
                },
                CraftingTime = 1.0f,
                OutputQuantity = 1
            };
            availableRecipes.Add(bandageRecipe);

            Log.Info($"Loaded {availableRecipes.Count} mock recipes.");
        }

        private void PopulateRecipeList()
        {
            if (recipeListPanel == null || RecipeListItemPrefab == null) return;

            recipeListPanel.Children.Clear();

            foreach (var recipe in availableRecipes)
            {
                var listItemEntityResult = RecipeListItemPrefab.Instantiate();
                if (listItemEntityResult == null || !listItemEntityResult.Any())
                {
                    Log.Error($"CraftingPanelScript: Failed to instantiate RecipeListItemPrefab for recipe '{recipe.DisplayName}'.");
                    continue;
                }
                var listItemEntity = listItemEntityResult.First();
                
                // Ensure the entity is added to a scene so its script can run Start()
                if (listItemEntity.Scene == null)
                {
                     this.Entity.Scene?.Entities.Add(listItemEntity);
                }

                var script = listItemEntity.Get<RecipeListItemScript>();
                if (script != null)
                {
                    script.Initialize(recipe, OnRecipeSelected);
                    var uiComponent = listItemEntity.Get<UIComponent>();
                    if (uiComponent?.Page?.RootElement != null)
                    {
                        recipeListPanel.Children.Add(uiComponent.Page.RootElement);
                    }
                    else
                    {
                        Log.Error($"CraftingPanelScript: Instantiated RecipeListItemPrefab for recipe '{recipe.DisplayName}' is missing UIComponent or Page setup.");
                        // If adding to scene, clean it up if it's not usable
                        this.Entity.Scene?.Entities.Remove(listItemEntity);
                    }
                }
                else
                {
                    Log.Error($"CraftingPanelScript: RecipeListItemScript not found on instantiated prefab for recipe '{recipe.DisplayName}'.");
                     // If adding to scene, clean it up if it's not usable
                    this.Entity.Scene?.Entities.Remove(listItemEntity);
                }
            }
        }

        private void OnRecipeSelected(CraftingRecipe recipe)
        {
            if (recipe == null) return;
            currentSelectedRecipe = recipe;

            if (selectedItemIcon != null)
            {
                if (recipe.Icon != null)
                {
                    selectedItemIcon.Source = new SpriteFromTexture(recipe.Icon);
                    selectedItemIcon.Visibility = Visibility.Visible;
                }
                else
                {
                    selectedItemIcon.Source = null; // Or a default placeholder icon
                    selectedItemIcon.Visibility = Visibility.Hidden; // Or some placeholder image
                }
            }
            if (selectedItemNameText != null) selectedItemNameText.Text = recipe.DisplayName ?? "N/A";
            if (selectedItemDescriptionText != null) selectedItemDescriptionText.Text = recipe.Description ?? "No description.";
            if (craftingTimeText != null) craftingTimeText.Text = $"Crafting Time: {recipe.CraftingTime:F1}s";

            // Populate required resources
            if (requiredResourcesPanel != null)
            {
                requiredResourcesPanel.Children.Clear();
                foreach (var res in recipe.RequiredResources)
                {
                    // Dynamically create TextBlocks for each resource.
                    // A prefab (RequiredResourceDisplayPrefab) would be better for more complex display.
                    var resourceText = new TextBlock
                    {
                        Text = $"{res.DisplayName}: {res.Quantity} (Owned: ?)", // Mock player inventory check
                        FontSize = 12,
                        TextColor = Color.FromAbgr(0xFFE0E0E0), // #FFE0E0E0
                        Margin = new Thickness(2,0,2,0)
                    };
                    requiredResourcesPanel.Children.Add(resourceText);
                }
            }

            // Update craft button state (mocked)
            if (craftButton != null)
            {
                // Mock: Check if player has resources. For now, always enable if a recipe is selected.
                bool canCraft = true; // Replace with actual inventory check logic
                craftButton.IsEnabled = canCraft;
                // Example: craftButton.Content = canCraft ? "Craft" : "Missing Resources";
            }
            Log.Info($"Recipe selected: {recipe.DisplayName}");
        }

        private void HandleCraftButtonClick(object sender, RoutedEventArgs args)
        {
            if (currentSelectedRecipe != null)
            {
                Log.Info($"Attempting to craft {currentSelectedRecipe.DisplayName}.");
                // Future: Trigger backend crafting logic, update inventory, manage crafting queue.
                // For now, maybe show a temporary "Crafted!" message or disable button briefly.
                if (craftButton != null)
                {
                    // Example: simple feedback
                    var originalText = craftButton.Content;
                    craftButton.Content = "Crafting...";
                    craftButton.IsEnabled = false;
                    
                    // Simulate crafting time and then revert
                    Entity.RunDelayed(() => {
                        if (craftButton != null) // Check if still valid
                        {
                             craftButton.Content = originalText; // Or "Crafted!" then revert after another delay
                             craftButton.IsEnabled = true; // Re-enable based on canCraft logic
                             Log.Info($"{currentSelectedRecipe.DisplayName} notionally crafted!");
                        }
                    }, currentSelectedRecipe.CraftingTime);
                }
            }
            else
            {
                Log.Warning("Craft button clicked but no recipe selected.");
            }
        }
        
        public override void Cancel()
        {
            if (craftButton != null)
            {
                craftButton.Click -= HandleCraftButtonClick;
            }
            // Any other cleanup
            base.Cancel();
        }
    }
}
