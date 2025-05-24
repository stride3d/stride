// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.UI;
using Stride.UI.Controls;
using Stride.UI.Panels; // For Canvas, StackPanel
using Stride.UnitTesting;
using System.Linq;
using System.Collections.Generic; // Required for List<Entity>

using FirstPersonShooter.UI.Scripts;
using FirstPersonShooter.Items.Engrams; // For EngramEntry

namespace FirstPersonShooter.Tests.UI
{
    [TestClass]
    public class EngramPanelTests : GameTestBase
    {
        private Scene testScene;
        private Entity panelEntity;
        private EngramPanelScript engramScript;

        // Mock UI Elements
        private Canvas mockEngramTreeCanvas;
        private ImageElement mockSelectedEngramIcon;
        private TextBlock mockSelectedEngramNameText, mockSelectedEngramDescriptionText;
        private TextBlock mockEngramCostText, mockRequiredLevelText, mockEngramPointsText;
        private StackPanel mockPrerequisitesPanel;
        private Button mockUnlockEngramButton;
        private TextBlock mockPrerequisitesHeader; // Added for completeness from EngramPanel.sdslui

        [TestInitialize]
        public void Setup()
        {
            testScene = new Scene();
            Game.SceneSystem.SceneInstance = new SceneInstance(Services, testScene);
            
            panelEntity = new Entity("EngramPanelEntity");
            engramScript = new EngramPanelScript();

            mockEngramTreeCanvas = new Canvas { Name = "EngramTreeCanvas" };
            mockSelectedEngramIcon = new ImageElement { Name = "SelectedEngramIcon" };
            mockSelectedEngramNameText = new TextBlock { Name = "SelectedEngramNameText" };
            mockSelectedEngramDescriptionText = new TextBlock { Name = "SelectedEngramDescriptionText" };
            mockEngramCostText = new TextBlock { Name = "EngramCostText" };
            mockRequiredLevelText = new TextBlock { Name = "RequiredLevelText" };
            mockEngramPointsText = new TextBlock { Name = "EngramPointsText" };
            mockPrerequisitesPanel = new StackPanel { Name = "PrerequisitesPanel" };
            mockUnlockEngramButton = new Button { Name = "UnlockEngramButton", IsEnabled = false };
            mockPrerequisitesHeader = new TextBlock { Name = "PrerequisitesHeader", Visibility = Visibility.Collapsed };


            var rootPanel = new Canvas { Name = "RootTestPanel" };
            rootPanel.Children.Add(mockEngramTreeCanvas);
            rootPanel.Children.Add(mockSelectedEngramIcon);
            rootPanel.Children.Add(mockSelectedEngramNameText);
            rootPanel.Children.Add(mockSelectedEngramDescriptionText);
            rootPanel.Children.Add(mockEngramCostText);
            rootPanel.Children.Add(mockRequiredLevelText);
            rootPanel.Children.Add(mockEngramPointsText);
            rootPanel.Children.Add(mockPrerequisitesPanel);
            rootPanel.Children.Add(mockUnlockEngramButton);
            rootPanel.Children.Add(mockPrerequisitesHeader);
            
            var uiComponent = new UIComponent { Page = new UIPage { RootElement = rootPanel } };
            panelEntity.Add(uiComponent);
            panelEntity.Add(engramScript);

            // Mock EngramNodePrefab
            var engramNodeEntityPrefab = new Entity("MockEngramNodePrefab");
            var engramNodeUI = new Button { Name = "EngramNodeButton" }; // Root of EngramNode.sdslui
            var engramNodeGrid = new Grid(); // Content of the button
            engramNodeGrid.Children.Add(new ImageElement { Name = "EngramIconImage" });
            engramNodeGrid.Children.Add(new TextBlock { Name = "EngramNameText" });
            engramNodeGrid.Children.Add(new Border { Name = "StatusIndicator" });
            engramNodeUI.Content = engramNodeGrid;
            
            engramNodeEntityPrefab.Add(new UIComponent { Page = new UIPage { RootElement = engramNodeUI } });
            engramNodeEntityPrefab.Add(new EngramNodeScript());
            engramScript.EngramNodePrefab = new Prefab(engramNodeEntityPrefab);
            
            testScene.Entities.Add(panelEntity);
        }

        [TestCleanup]
        public void Teardown()
        {
            if (panelEntity != null) testScene.Entities.Remove(panelEntity);
            // Nullify mocks
            panelEntity = null;
            engramScript = null;
            // ... nullify all mock UI elements ...

            Game.SceneSystem.SceneInstance = null;
            testScene?.Dispose();
            testScene = null;
        }

        [TestMethod]
        public void TestEngramPanelInitialization()
        {
            // Set mock player data before Start
            engramScript.SetPrivateField("currentPlayerLevel", 1);
            engramScript.SetPrivateField("currentPlayerEngramPoints", 10);
            
            engramScript.Start();

            var allEngrams = engramScript.GetPrivateField<List<EngramEntry>>("allEngrams");
            Assert.IsTrue(allEngrams.Count > 0, "Engrams should be loaded.");
            Assert.AreEqual(allEngrams.Count, mockEngramTreeCanvas.Children.Count, "EngramTreeCanvas should be populated.");
            Assert.AreEqual("10 Points", mockEngramPointsText.Text, "Player engram points text mismatch.");
            
            var currentSelectedEngram = engramScript.GetPrivateField<EngramEntry>("currentSelectedEngram");
            Assert.IsNotNull(currentSelectedEngram, "An engram should be selected by default.");
        }

        [TestMethod]
        public void TestEngramSelectionUpdatesDetails()
        {
            engramScript.SetPrivateField("currentPlayerLevel", 5);
            engramScript.SetPrivateField("currentPlayerEngramPoints", 20);
            engramScript.Start();
            
            var allEngrams = engramScript.GetPrivateField<List<EngramEntry>>("allEngrams");
            Assert.IsTrue(allEngrams.Count > 0, "No engrams loaded for selection test.");

            EngramEntry engramToSelect = allEngrams.First(e => e.EngramID == "Spear"); // Known mock engram
            engramScript.CallPrivateMethod("OnEngramSelected", engramToSelect);

            var currentSelectedEngram = engramScript.GetPrivateField<EngramEntry>("currentSelectedEngram");
            Assert.AreSame(engramToSelect, currentSelectedEngram, "currentSelectedEngram not updated.");
            Assert.AreEqual(engramToSelect.DisplayName, mockSelectedEngramNameText.Text, "Selected engram name mismatch.");
            Assert.AreEqual(engramToSelect.Description, mockSelectedEngramDescriptionText.Text, "Selected engram description mismatch.");
            Assert.AreEqual($"{engramToSelect.EngramPointCost} EP", mockEngramCostText.Text, "Engram cost text mismatch.");
            Assert.AreEqual($"Lvl {engramToSelect.RequiredPlayerLevel}", mockRequiredLevelText.Text, "Required level text mismatch.");

            // Prerequisites check
            if (engramToSelect.PrerequisiteEngramIDs.Any())
            {
                Assert.AreEqual(Visibility.Visible, mockPrerequisitesHeader.Visibility, "Prerequisites header should be visible.");
                Assert.AreEqual(engramToSelect.PrerequisiteEngramIDs.Count, mockPrerequisitesPanel.Children.Count, "PrerequisitesPanel items count mismatch.");
            }
            else
            {
                Assert.AreEqual(Visibility.Collapsed, mockPrerequisitesHeader.Visibility, "Prerequisites header should be collapsed.");
            }
            
            // Button state check (Spear is initially Unlockable if WoodenClub is unlocked, and player level/points are sufficient)
            // To test this accurately, we'd need to ensure WoodenClub is Unlocked first.
            // For simplicity here, we assume UpdateAllEngramStatuses correctly sets it to Unlockable.
            engramToSelect.Status = EngramStatus.Unlockable; // Force status for this part of test
             engramScript.CallPrivateMethod("OnEngramSelected", engramToSelect); // Re-select to update button
            Assert.IsTrue(mockUnlockEngramButton.IsEnabled, "Unlock button should be enabled for an unlockable engram with sufficient points/level.");
        }

        [TestMethod]
        public void TestEngramUnlockLogic()
        {
            engramScript.SetPrivateField("currentPlayerLevel", 3);
            engramScript.SetPrivateField("currentPlayerEngramPoints", 5);
            engramScript.Start(); // Loads engrams, updates statuses

            var spearEngram = engramScript.GetPrivateField<List<EngramEntry>>("allEngrams").First(e => e.EngramID == "Spear");
            // Manually unlock prerequisite "WoodenClub" for this test flow
            var clubEngram = engramScript.GetPrivateField<List<EngramEntry>>("allEngrams").First(e => e.EngramID == "WoodenClub");
            clubEngram.Status = EngramStatus.Unlocked; 
            engramScript.CallPrivateMethod("UpdateAllEngramStatuses", false); // Update statuses based on new state (no visual refresh needed here)
            
            spearEngram = engramScript.GetPrivateField<List<EngramEntry>>("allEngrams").First(e => e.EngramID == "Spear"); // Re-fetch to get updated status
            Assert.AreEqual(EngramStatus.Unlockable, spearEngram.Status, "Spear engram should be Unlockable now.");

            engramScript.CallPrivateMethod("OnEngramSelected", spearEngram); // Select it, this will update button state

            Assert.IsTrue(mockUnlockEngramButton.IsEnabled, "Unlock button should be enabled for Spear.");
            
            int initialPlayerPoints = (int)engramScript.GetPrivateField("currentPlayerEngramPoints");

            // Simulate click
            engramScript.CallPrivateMethod("HandleUnlockEngramClick", mockUnlockEngramButton, new RoutedEventArgs());

            Assert.AreEqual(EngramStatus.Unlocked, spearEngram.Status, "Spear engram should be Unlocked after click.");
            Assert.AreEqual(initialPlayerPoints - spearEngram.EngramPointCost, (int)engramScript.GetPrivateField("currentPlayerEngramPoints"), "Player points not deducted correctly.");
            Assert.AreEqual("Unlocked", mockUnlockEngramButton.Content?.ToString(), "Unlock button text should change to Unlocked.");
            Assert.IsFalse(mockUnlockEngramButton.IsEnabled, "Unlock button should be disabled after unlocking.");
        }

        [TestMethod]
        public void TestEngramStatusUpdates()
        {
            // Player starts at level 1, 0 points for this specific test to check progression
            engramScript.SetPrivateField("currentPlayerLevel", 1);
            engramScript.SetPrivateField("currentPlayerEngramPoints", 10); // Enough points for testing unlocks
            engramScript.Start(); // This calls LoadMockEngrams and UpdateAllEngramStatuses

            var allEngrams = engramScript.GetPrivateField<List<EngramEntry>>("allEngrams");
            var thatchFoundation = allEngrams.First(e => e.EngramID == "ThatchFoundation");
            var thatchWall = allEngrams.First(e => e.EngramID == "ThatchWall");

            // Initial state: Player level 1
            // ThatchFoundation: Lvl 1, 1 EP -> Unlockable
            // ThatchWall: Lvl 2, 1 EP, Prereq: ThatchFoundation -> Locked (due to level and prereq)
            Assert.AreEqual(EngramStatus.Unlockable, thatchFoundation.Status, "ThatchFoundation should be Unlockable at Lvl 1.");
            Assert.AreEqual(EngramStatus.Locked, thatchWall.Status, "ThatchWall should be Locked at Lvl 1.");

            // Increase player level to 2 (ThatchWall now meets level req, but not prereq status)
            engramScript.SetPrivateField("currentPlayerLevel", 2);
            engramScript.CallPrivateMethod("UpdateAllEngramStatuses", true); // Refresh statuses and node visuals
            Assert.AreEqual(EngramStatus.Locked, thatchWall.Status, "ThatchWall should still be Locked (prereq not met).");

            // Unlock ThatchFoundation
            thatchFoundation.Status = EngramStatus.Unlocked; // Simulate unlock (usually via HandleUnlockEngramClick)
            engramScript.SetPrivateField("currentPlayerEngramPoints", 
                (int)engramScript.GetPrivateField("currentPlayerEngramPoints") - thatchFoundation.EngramPointCost); // Manually deduct points
            
            engramScript.CallPrivateMethod("UpdateAllEngramStatuses", true); // Refresh statuses
            
            Assert.AreEqual(EngramStatus.Unlockable, thatchWall.Status, "ThatchWall should now be Unlockable.");
        }
    }
}
