// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using Stride.Graphics; // For Texture
using Stride.UI;
using Stride.UI.Controls; // Image, TextBlock, Button, ScrollViewer, Canvas
using Stride.UI.Panels;   // StackPanel, Canvas
using Stride.UI.Events;   // RoutedEventArgs
using Stride.Core.Mathematics; // For Vector2
using System.Collections.Generic;
using System.Linq; // For FirstOrDefault
using FirstPersonShooter.Items.Engrams; // For EngramEntry, EngramStatus

namespace FirstPersonShooter.UI.Scripts
{
    public class EngramPanelScript : UIScript
    {
        // Public properties - assign in Stride Editor
        public Prefab EngramNodePrefab { get; set; }

        // Private fields for UI elements
        private Canvas engramTreeCanvas;
        private ImageElement selectedEngramIcon;
        private TextBlock selectedEngramNameText, selectedEngramDescriptionText, engramCostText, requiredLevelText, engramPointsText;
        private StackPanel prerequisitesPanel;
        private Button unlockEngramButton;
        
        private UIElement rootElement;

        private List<EngramEntry> allEngrams = new List<EngramEntry>();
        private Dictionary<string, EngramNodeScript> engramNodeScripts = new Dictionary<string, EngramNodeScript>();
        private EngramEntry currentSelectedEngram = null;
        
        // Mock player data
        private int currentPlayerLevel = 5; 
        private int currentPlayerEngramPoints = 20;

        // (Advanced) Deferring connectionLines for now
        // private List<Stride.UI.Controls.Shape> connectionLines = new List<Shape>();

        public override void Start()
        {
            base.Start();

            rootElement = Entity.Get<UIComponent>()?.Page?.RootElement;
            if (rootElement == null)
            {
                Log.Error("EngramPanelScript: Root UI element not found.");
                return;
            }

            // Find UI elements
            engramTreeCanvas = rootElement.FindName<Canvas>("EngramTreeCanvas");
            selectedEngramIcon = rootElement.FindName<ImageElement>("SelectedEngramIcon");
            selectedEngramNameText = rootElement.FindName<TextBlock>("SelectedEngramNameText");
            selectedEngramDescriptionText = rootElement.FindName<TextBlock>("SelectedEngramDescriptionText");
            engramCostText = rootElement.FindName<TextBlock>("EngramCostText");
            requiredLevelText = rootElement.FindName<TextBlock>("RequiredLevelText");
            engramPointsText = rootElement.FindName<TextBlock>("EngramPointsText");
            prerequisitesPanel = rootElement.FindName<StackPanel>("PrerequisitesPanel");
            unlockEngramButton = rootElement.FindName<Button>("UnlockEngramButton");

            // Log errors if critical elements are not found
            if (engramTreeCanvas == null) Log.Error("EngramPanelScript: EngramTreeCanvas not found.");
            if (selectedEngramIcon == null) Log.Error("EngramPanelScript: SelectedEngramIcon not found.");
            // ... (add more null checks for other critical UI elements as needed)
            if (unlockEngramButton == null) Log.Error("EngramPanelScript: UnlockEngramButton not found.");
            if (engramPointsText == null) Log.Error("EngramPanelScript: EngramPointsText not found.");


            if (EngramNodePrefab == null)
            {
                Log.Error("EngramPanelScript: EngramNodePrefab is not assigned. Please assign it in the editor.");
                return;
            }

            if (engramPointsText != null) // Initial update for player's engram points
            {
                engramPointsText.Text = $"{currentPlayerEngramPoints} Points";
            }

            LoadMockEngrams();
            UpdateAllEngramStatuses(); // Initial status update based on player level and unlocked prerequisites
            PopulateEngramTree();

            if (unlockEngramButton != null)
            {
                unlockEngramButton.Click += HandleUnlockEngramClick;
            }

            // Select a default engram to display details for
            if (allEngrams.Count > 0)
            {
                OnEngramSelected(allEngrams.FirstOrDefault(e => e.Status != EngramStatus.Locked) ?? allEngrams[0]);
            }
        }

        private void LoadMockEngrams()
        {
            allEngrams.Clear();

            allEngrams.Add(new EngramEntry { 
                EngramID = "ThatchFoundation", DisplayName = "Thatch Foundation", Description = "A simple foundation made of thatch.", 
                Icon = null, EngramPointCost = 1, RequiredPlayerLevel = 1, 
                UIPosition = new Vector2(50, 50), UnlocksRecipeIDs = new List<string>{"ThatchFoundationRecipe"}
            });
            allEngrams.Add(new EngramEntry { 
                EngramID = "ThatchWall", DisplayName = "Thatch Wall", Description = "A basic wall for shelter.", 
                Icon = null, EngramPointCost = 1, RequiredPlayerLevel = 2, 
                PrerequisiteEngramIDs = new List<string> { "ThatchFoundation" }, 
                UIPosition = new Vector2(50, 120), UnlocksRecipeIDs = new List<string>{"ThatchWallRecipe"}
            });
             allEngrams.Add(new EngramEntry { 
                EngramID = "ThatchCeiling", DisplayName = "Thatch Ceiling", Description = "A basic ceiling for shelter.", 
                Icon = null, EngramPointCost = 1, RequiredPlayerLevel = 2, 
                PrerequisiteEngramIDs = new List<string> { "ThatchWall" }, 
                UIPosition = new Vector2(50, 190), UnlocksRecipeIDs = new List<string>{"ThatchCeilingRecipe"}
            });
            allEngrams.Add(new EngramEntry { 
                EngramID = "WoodenClub", DisplayName = "Wooden Club", Description = "A simple blunt weapon.", 
                Icon = null, EngramPointCost = 2, RequiredPlayerLevel = 1, 
                UIPosition = new Vector2(250, 50), UnlocksRecipeIDs = new List<string>{"WoodenClubRecipe"}
            });
             allEngrams.Add(new EngramEntry { 
                EngramID = "Spear", DisplayName = "Wooden Spear", Description = "A throwable spear for hunting.", 
                Icon = null, EngramPointCost = 3, RequiredPlayerLevel = 3, 
                PrerequisiteEngramIDs = new List<string> { "WoodenClub" },
                UIPosition = new Vector2(250, 120), UnlocksRecipeIDs = new List<string>{"SpearRecipe"}
            });

            Log.Info($"Loaded {allEngrams.Count} mock engrams.");
        }

        private void UpdateAllEngramStatuses(bool refreshNodesVisuals = false)
        {
            bool statusChanged;
            do // Loop until no more statuses change, to handle multi-level prerequisite updates
            {
                statusChanged = false;
                foreach (var engram in allEngrams)
                {
                    if (engram.Status == EngramStatus.Unlocked) continue;

                    var oldStatus = engram.Status;
                    bool canUnlock = currentPlayerLevel >= engram.RequiredPlayerLevel;
                    if (canUnlock)
                    {
                        foreach (var prereqID in engram.PrerequisiteEngramIDs)
                        {
                            var prereqEngram = allEngrams.FirstOrDefault(e => e.EngramID == prereqID);
                            if (prereqEngram == null || prereqEngram.Status != EngramStatus.Unlocked)
                            {
                                canUnlock = false;
                                break;
                            }
                        }
                    }

                    EngramStatus newStatus = canUnlock ? EngramStatus.Unlockable : EngramStatus.Locked;
                    if (engram.Status != newStatus)
                    {
                        engram.Status = newStatus;
                        statusChanged = true;
                        if (refreshNodesVisuals && engramNodeScripts.TryGetValue(engram.EngramID, out var nodeScript))
                        {
                            nodeScript.UpdateStatusVisuals();
                        }
                    }
                }
            } while (statusChanged);
        }


        private void PopulateEngramTree()
        {
            if (engramTreeCanvas == null || EngramNodePrefab == null) return;

            // Clear existing nodes (but not lines, if managed separately)
            // For now, clearing all children as lines are not implemented.
            engramTreeCanvas.Children.Clear();
            engramNodeScripts.Clear();

            foreach (var engram in allEngrams)
            {
                var nodeEntityResult = EngramNodePrefab.Instantiate();
                if (nodeEntityResult == null || !nodeEntityResult.Any())
                {
                    Log.Error($"EngramPanelScript: Failed to instantiate EngramNodePrefab for engram '{engram.DisplayName}'.");
                    continue;
                }
                var nodeEntity = nodeEntityResult.First();
                
                if (nodeEntity.Scene == null)
                {
                    this.Entity.Scene?.Entities.Add(nodeEntity);
                }

                var nodeScript = nodeEntity.Get<EngramNodeScript>();
                if (nodeScript != null)
                {
                    nodeScript.Initialize(engram, OnEngramSelected);
                    var uiComponent = nodeEntity.Get<UIComponent>();
                    if (uiComponent?.Page?.RootElement != null)
                    {
                        uiComponent.Page.RootElement.SetCanvasPosition(engram.UIPosition);
                        engramTreeCanvas.Children.Add(uiComponent.Page.RootElement);
                        engramNodeScripts[engram.EngramID] = nodeScript;
                    }
                    else
                    {
                        Log.Error($"EngramPanelScript: Instantiated EngramNodePrefab for engram '{engram.DisplayName}' is missing UIComponent or Page setup.");
                        this.Entity.Scene?.Entities.Remove(nodeEntity);
                    }
                }
                else
                {
                    Log.Error($"EngramPanelScript: EngramNodeScript not found on EngramNodePrefab for engram '{engram.DisplayName}'.");
                    this.Entity.Scene?.Entities.Remove(nodeEntity);
                }
            }
            // (Advanced) Call DrawConnectionLines();
        }

        private void OnEngramSelected(EngramEntry engram)
        {
            if (engram == null) return;
            currentSelectedEngram = engram;

            if (selectedEngramIcon != null)
            {
                selectedEngramIcon.Source = (engram.Icon != null) ? new SpriteFromTexture(engram.Icon) : null;
                selectedEngramIcon.Visibility = (engram.Icon != null) ? Visibility.Visible : Visibility.Hidden;
            }
            if (selectedEngramNameText != null) selectedEngramNameText.Text = engram.DisplayName ?? "N/A";
            if (selectedEngramDescriptionText != null) selectedEngramDescriptionText.Text = engram.Description ?? "No description.";
            if (engramCostText != null) engramCostText.Text = $"{engram.EngramPointCost} EP";
            if (requiredLevelText != null) requiredLevelText.Text = $"Lvl {engram.RequiredPlayerLevel}";

            // Populate prerequisites
            var prereqHeader = rootElement?.FindName<TextBlock>("PrerequisitesHeader");
            if (prerequisitesPanel != null)
            {
                prerequisitesPanel.Children.Clear();
                if (engram.PrerequisiteEngramIDs.Any())
                {
                    if (prereqHeader != null) prereqHeader.Visibility = Visibility.Visible;
                    foreach (var prereqID in engram.PrerequisiteEngramIDs)
                    {
                        var prereqEngram = allEngrams.FirstOrDefault(e => e.EngramID == prereqID);
                        var prereqText = new TextBlock { 
                            Text = $"- {prereqEngram?.DisplayName ?? prereqID}", 
                            FontSize = 12, 
                            TextColor = (prereqEngram?.Status == EngramStatus.Unlocked) ? Colors.LightGreen : Colors.OrangeRed,
                            Margin = new Thickness(0,0,0,2)
                        };
                        prerequisitesPanel.Children.Add(prereqText);
                    }
                }
                else
                {
                     if (prereqHeader != null) prereqHeader.Visibility = Visibility.Collapsed;
                }
            }

            // Update unlock button state
            if (unlockEngramButton != null)
            {
                unlockEngramButton.IsEnabled = (engram.Status == EngramStatus.Unlockable && currentPlayerEngramPoints >= engram.EngramPointCost);
                unlockEngramButton.Content = (engram.Status == EngramStatus.Unlocked) ? "Unlocked" : "Unlock";
            }
            Log.Info($"Engram selected: {engram.DisplayName}, Status: {engram.Status}");
        }

        private void HandleUnlockEngramClick(object sender, RoutedEventArgs args)
        {
            if (currentSelectedEngram == null) return;

            if (currentSelectedEngram.Status == EngramStatus.Unlockable && 
                currentPlayerEngramPoints >= currentSelectedEngram.EngramPointCost &&
                currentPlayerLevel >= currentSelectedEngram.RequiredPlayerLevel) // Double check level
            {
                currentPlayerEngramPoints -= currentSelectedEngram.EngramPointCost;
                currentSelectedEngram.Status = EngramStatus.Unlocked;
                Log.Info($"Unlocked {currentSelectedEngram.DisplayName}");

                if (engramPointsText != null) engramPointsText.Text = $"{currentPlayerEngramPoints} Points";

                // Refresh status of the just-unlocked node
                if (engramNodeScripts.TryGetValue(currentSelectedEngram.EngramID, out var unlockedNodeScript))
                {
                    unlockedNodeScript.UpdateStatusVisuals();
                }

                // Update statuses of all other engrams as this unlock might make new ones unlockable
                UpdateAllEngramStatuses(true); // Pass true to refresh visuals of affected nodes

                // Refresh the selected engram details (especially button state)
                OnEngramSelected(currentSelectedEngram); 
                
                // (Future: Save unlocked state to player profile)
            }
            else
            {
                Log.Warning($"Cannot unlock {currentSelectedEngram.DisplayName}. Conditions not met. " +
                            $"Status: {currentSelectedEngram.Status}, Points: {currentPlayerEngramPoints}/{currentSelectedEngram.EngramPointCost}, Level: {currentPlayerLevel}/{currentSelectedEngram.RequiredPlayerLevel}");
            }
        }
        
        public override void Cancel()
        {
            if (unlockEngramButton != null)
            {
                unlockEngramButton.Click -= HandleUnlockEngramClick;
            }
            // Any other cleanup
            base.Cancel();
        }
    }
}
