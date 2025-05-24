// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels;
using Stride.UnitTesting;
using System.Linq;

using FirstPersonShooter.UI.Scripts;
using FirstPersonShooter.Items.Crafting; // For CraftingRecipe
using System.Collections.Generic; // Required for List<Entity>

namespace FirstPersonShooter.Tests.UI
{
    [TestClass]
    public class CraftingPanelTests : GameTestBase
    {
        private Scene testScene;
        private Entity panelEntity;
        private CraftingPanelScript craftingScript;

        // Mock UI Elements
        private StackPanel mockRecipeListPanel;
        private ImageElement mockSelectedItemIcon;
        private TextBlock mockSelectedItemNameText;
        private TextBlock mockSelectedItemDescriptionText;
        private StackPanel mockRequiredResourcesPanel;
        private TextBlock mockCraftingTimeText;
        private Button mockCraftButton;

        [TestInitialize]
        public void Setup()
        {
            testScene = new Scene();
            Game.SceneSystem.SceneInstance = new SceneInstance(Services, testScene);
            
            panelEntity = new Entity("CraftingPanelEntity");
            craftingScript = new CraftingPanelScript();

            mockRecipeListPanel = new StackPanel { Name = "RecipeListPanel" };
            mockSelectedItemIcon = new ImageElement { Name = "SelectedItemIcon" };
            mockSelectedItemNameText = new TextBlock { Name = "SelectedItemNameText" };
            mockSelectedItemDescriptionText = new TextBlock { Name = "SelectedItemDescriptionText" };
            mockRequiredResourcesPanel = new StackPanel { Name = "RequiredResourcesPanel" };
            mockCraftingTimeText = new TextBlock { Name = "CraftingTimeText" };
            mockCraftButton = new Button { Name = "CraftButton", IsEnabled = false }; // Start disabled

            var rootPanel = new Canvas { Name = "RootTestPanel" }; // Or Grid
            rootPanel.Children.Add(mockRecipeListPanel);
            rootPanel.Children.Add(mockSelectedItemIcon);
            rootPanel.Children.Add(mockSelectedItemNameText);
            rootPanel.Children.Add(mockSelectedItemDescriptionText);
            rootPanel.Children.Add(mockRequiredResourcesPanel);
            rootPanel.Children.Add(mockCraftingTimeText);
            rootPanel.Children.Add(mockCraftButton);
            
            var uiComponent = new UIComponent { Page = new UIPage { RootElement = rootPanel } };
            panelEntity.Add(uiComponent);
            panelEntity.Add(craftingScript);

            // Mock RecipeListItemPrefab
            var recipeItemEntityPrefab = new Entity("MockRecipeItemPrefab");
            var recipeItemUI = new Button { Name = "RecipeButton" }; // Root of RecipeListItem.sdslui is a Button
            recipeItemUI.Content = new Grid(); // Add a grid to hold icon and text for FindName
            ((Grid)recipeItemUI.Content).Children.Add(new ImageElement { Name = "ItemIconImage" });
            ((Grid)recipeItemUI.Content).Children.Add(new TextBlock { Name = "ItemNameText" });
            
            recipeItemEntityPrefab.Add(new UIComponent { Page = new UIPage { RootElement = recipeItemUI } });
            recipeItemEntityPrefab.Add(new RecipeListItemScript());
            craftingScript.RecipeListItemPrefab = new Prefab(recipeItemEntityPrefab);
            
            testScene.Entities.Add(panelEntity);
        }

        [TestCleanup]
        public void Teardown()
        {
            if (panelEntity != null) testScene.Entities.Remove(panelEntity);
            // Nullify mocks and script
            panelEntity = null;
            craftingScript = null;
            // ... nullify all mock UI elements ...
            
            Game.SceneSystem.SceneInstance = null;
            testScene?.Dispose();
            testScene = null;
        }

        [TestMethod]
        public void TestCraftingPanelInitialization()
        {
            craftingScript.Start();

            var availableRecipes = craftingScript.GetPrivateField<List<CraftingRecipe>>("availableRecipes");
            Assert.IsTrue(availableRecipes.Count > 0, "Available recipes should be loaded.");
            Assert.AreEqual(availableRecipes.Count, mockRecipeListPanel.Children.Count, "RecipeListPanel should be populated with recipes.");
            
            var currentSelectedRecipe = craftingScript.GetPrivateField<CraftingRecipe>("currentSelectedRecipe");
            Assert.IsNotNull(currentSelectedRecipe, "A recipe should be selected by default.");
            Assert.AreEqual(availableRecipes[0].DisplayName, mockSelectedItemNameText.Text, "Default selected item name mismatch.");
        }

        [TestMethod]
        public void TestRecipeSelectionUpdatesDetails()
        {
            craftingScript.Start();
            var availableRecipes = craftingScript.GetPrivateField<List<CraftingRecipe>>("availableRecipes");
            Assert.IsTrue(availableRecipes.Count >= 2, "Need at least two recipes for this test.");

            CraftingRecipe recipeToSelect = availableRecipes[1]; // Select the second recipe
            craftingScript.CallPrivateMethod("OnRecipeSelected", recipeToSelect); // Call directly as it's the core logic

            var currentSelectedRecipe = craftingScript.GetPrivateField<CraftingRecipe>("currentSelectedRecipe");
            Assert.AreSame(recipeToSelect, currentSelectedRecipe, "currentSelectedRecipe not updated.");
            Assert.AreEqual(recipeToSelect.DisplayName, mockSelectedItemNameText.Text, "Selected item name mismatch.");
            Assert.AreEqual(recipeToSelect.Description, mockSelectedItemDescriptionText.Text, "Selected item description mismatch.");
            Assert.AreEqual($"Crafting Time: {recipeToSelect.CraftingTime:F1}s", mockCraftingTimeText.Text, "Crafting time text mismatch.");
            
            Assert.AreEqual(recipeToSelect.RequiredResources.Count, mockRequiredResourcesPanel.Children.Count, "RequiredResourcesPanel items count mismatch.");
            if (recipeToSelect.RequiredResources.Any())
            {
                var firstResText = mockRequiredResourcesPanel.Children[0] as TextBlock;
                Assert.IsNotNull(firstResText, "First resource TextBlock is null.");
                StringAssert.Contains(firstResText.Text, recipeToSelect.RequiredResources[0].DisplayName, "First resource text mismatch.");
            }
            
            // Assuming mock logic enables button if recipe is selected
            Assert.IsTrue(mockCraftButton.IsEnabled, "Craft button should be enabled for a selectable recipe (mock logic).");
        }
        
        private bool craftButtonClickedFlag = false;
        private void TestCraftButtonHandler(object sender, RoutedEventArgs e)
        {
            craftButtonClickedFlag = true;
        }

        [TestMethod]
        public void TestCraftButtonClick()
        {
            craftingScript.Start();
            var availableRecipes = craftingScript.GetPrivateField<List<CraftingRecipe>>("availableRecipes");
            Assert.IsTrue(availableRecipes.Count > 0, "No recipes available for craft button test.");

            craftingScript.CallPrivateMethod("OnRecipeSelected", availableRecipes[0]); // Select first recipe
            
            // Mocking the craft button's click event subscription is tricky here.
            // Instead, we can check the state changes that HandleCraftButtonClick performs.
            // For this test, we'll focus on the log message and the button's IsEnabled state change.
            
            string originalButtonText = mockCraftButton.Content?.ToString() ?? "Craft"; // Assuming Content is string
            bool initialButtonState = mockCraftButton.IsEnabled;

            craftingScript.CallPrivateMethod("HandleCraftButtonClick", mockCraftButton, new RoutedEventArgs());
            
            // After click, button text should change and become disabled during "crafting"
            Assert.AreNotEqual(originalButtonText, mockCraftButton.Content?.ToString(), "Button text should change on click.");
            Assert.IsFalse(mockCraftButton.IsEnabled, "Button should be disabled during mock crafting.");

            // We can't easily test the Entity.RunDelayed part in a simple unit test frame.
            // This test verifies the immediate effects of the click.
            // To test RunDelayed, an integration-style test with time progression is needed.
            Log.Info("TestCraftButtonClick: Verified immediate effects. RunDelayed callback test needs integration setup.");
        }
    }
}
