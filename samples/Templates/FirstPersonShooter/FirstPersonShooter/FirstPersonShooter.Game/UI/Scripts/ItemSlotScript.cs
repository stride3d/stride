// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics; // For Texture, Sprite
using Stride.UI;
using Stride.UI.Controls; // For ImageElement, TextBlock, ProgressBar
using Stride.UI.Events;   // For PointerEventArgs
using Stride.UI.Panels;   // For Grid (if RootPanel is needed)
using Stride.Input;       // For Input (used in InventoryPanelScript, good to have consistent usings)

namespace FirstPersonShooter.UI.Scripts
{
    public class ItemSlotScript : UIScript
    {
        public ImageElement ItemIconImage { get; set; }
        public TextBlock QuantityText { get; set; }
        public ProgressBar DurabilityBar { get; set; }
        
        // Internal reference to the root of this slot's UI, typically a Grid.
        public UIElement RootElement { get; private set; } // Made public getter for bounds checking

        // Fields for drag state
        private bool isDragging = false;
        public Vector2 DragOffset { get; private set; } // Public for InventoryPanelScript to use
        public static ItemSlotScript CurrentlyDraggedSlot { get; private set; }
        public Items.MockInventoryItem ItemData { get; set; } // Changed type from object

        private InventoryPanelScript parentPanelScript;

        public override void Start()
        {
            base.Start(); // Important for UIScript initialization

            // Assuming this script is attached to an Entity that has a UIComponent,
            // and that UIComponent's Page is set to the ItemSlot.sdslui.
            // The RootElement of the Page is the Grid "RootPanel".
            RootElement = Entity.Get<UIComponent>()?.Page?.RootElement;

            if (RootElement == null)
            {
                Log.Error("ItemSlotScript: Could not find the root UI element for this slot.");
                return;
            }

            // Find UI elements by name from the root panel
            ItemIconImage = RootElement.FindName<ImageElement>("ItemIconImage");
            QuantityText = RootElement.FindName<TextBlock>("QuantityText");
            DurabilityBar = RootElement.FindName<ProgressBar>("DurabilityBar");

            if (ItemIconImage == null) Log.Error("ItemSlotScript: ItemIconImage not found in UI.");
            if (QuantityText == null) Log.Error("ItemSlotScript: QuantityText not found in UI.");
            if (DurabilityBar == null) Log.Error("ItemSlotScript: DurabilityBar not found in UI.");

            // Initialize slot as empty
            ClearSlot();

            // Find parent InventoryPanelScript
            var current = this.Entity.GetParent();
            while(current != null) 
            {
                parentPanelScript = current.Get<InventoryPanelScript>();
                if (parentPanelScript != null) break;
                current = current.GetParent();
            }
            // A more direct way if InventoryPanelScript is on a known entity (e.g. a root UI entity):
            // parentPanelScript = Entity.Scene.RootEntities.FirstOrDefault(e => e.Get<InventoryPanelScript>() != null)?.Get<InventoryPanelScript>();
            if (parentPanelScript == null) Log.Error($"ItemSlotScript on '{Entity.Name}' could not find parent InventoryPanelScript!");
        }

        /// <summary>
        /// Sets the item data for this slot, updating its visual representation.
        /// </summary>
        /// <param name="iconTexture">The texture for the item's icon.</param>
        /// <param name="quantity">The quantity of the item.</param>
        /// <param name="durability">The item's durability (0.0 to 1.0). Null if item has no durability.</param>
        /// <param name="itemObject">The actual data object for the item of type MockInventoryItem.</param>
        public void SetItemData(Texture iconTexture, int quantity, float? durability, Items.MockInventoryItem itemObject = null)
        {
            ItemData = itemObject; 
            
            if (ItemIconImage != null)
            {
                if (iconTexture != null)
                {
                    ItemIconImage.Source = new SpriteFromTexture(iconTexture);
                    ItemIconImage.Visibility = Visibility.Visible;
                }
                else
                {
                    ItemIconImage.Source = null;
                    ItemIconImage.Visibility = Visibility.Collapsed;
                }
            }

            if (QuantityText != null)
            {
                QuantityText.Text = quantity > 1 ? quantity.ToString() : "";
                QuantityText.Visibility = quantity > 1 ? Visibility.Visible : Visibility.Hidden;
            }

            if (DurabilityBar != null)
            {
                if (durability.HasValue && durability > 0f && durability < 1f) // Show only if relevant
                {
                    DurabilityBar.Visibility = Visibility.Visible;
                    DurabilityBar.Value = durability.Value;
                }
                else
                {
                    DurabilityBar.Visibility = Visibility.Collapsed;
                }
            }
        }
        
        // Helper methods to get current visual data for swapping
        public Texture GetIconTexture() => (ItemIconImage?.Source as SpriteFromTexture)?.Texture;
        public int GetQuantity() => ItemData?.Quantity ?? (int.TryParse(QuantityText?.Text, out int qty) ? qty : 0);
        public float? GetDurability() => ItemData?.Durability ?? (DurabilityBar?.Visibility == Visibility.Visible ? (float?)DurabilityBar.Value : null);


        /// <summary>
        /// Clears the item slot, making it appear empty.
        /// </summary>
        public void ClearSlot()
        {
            ItemData = null; // Clear the data object
            if (ItemIconImage != null)
            {
                ItemIconImage.Source = null;
                ItemIconImage.Visibility = Visibility.Collapsed;
            }
            if (QuantityText != null)
            {
                QuantityText.Text = "";
                QuantityText.Visibility = Visibility.Hidden;
            }
            if (DurabilityBar != null)
            {
                DurabilityBar.Visibility = Visibility.Collapsed;
            }
        }

        public override void OnPointerPressed(PointerEventArgs args)
        {
            base.OnPointerPressed(args);
            // Log.Info($"ItemSlot '{this.Entity.Name}': Pointer Pressed. Button: {args.MouseButton}");

            if (args.MouseButton == MouseButton.Left && (ItemData != null || ItemIconImage?.Source != null))
            {
                isDragging = true;
                CurrentlyDraggedSlot = this;
                
                // Calculate offset from the top-left of the slot to the mouse click position
                Vector3 slotPosition = RootElement.ActualPosition; // This is relative to parent
                Vector2 absoluteSlotPosition = new Vector2(RootElement.GetAbsolutePosition().X, RootElement.GetAbsolutePosition().Y);
                DragOffset = args.MousePosition - absoluteSlotPosition;

                // Visual indication (handled by InventoryPanelScript now)
                // RootElement.Opacity = 0.7f; 
                
                parentPanelScript?.HandleDragStarted(this, args.MousePosition);
                args.Handled = true;
            }
            else if (args.MouseButton == MouseButton.Right)
            {
                OnRightClick(args);
            }
        }

        public override void OnPointerReleased(PointerEventArgs args)
        {
            base.OnPointerReleased(args);
            // Log.Info($"ItemSlot '{this.Entity.Name}': Pointer Released. Button: {args.MouseButton}");

            if (isDragging && args.MouseButton == MouseButton.Left)
            {
                isDragging = false;
                parentPanelScript?.HandleDragReleased(this, args.MousePosition);
                // Visual reset (handled by InventoryPanelScript now)
                // RootElement.Opacity = 1.0f;
                CurrentlyDraggedSlot = null;
                args.Handled = true;
            }
        }

        public override void OnPointerEnter(PointerEventArgs args)
        {
            base.OnPointerEnter(args);
            // Log.Info($"ItemSlot '{this.Entity.Name}': Pointer Enter.");
            parentPanelScript?.HandleSlotPointerEnter(this);
            // Example: Change background on hover if not dragging something else
            if (CurrentlyDraggedSlot == null && RootElement is Panel panel) { 
                // panel.BackgroundColor = new Color(80,80,80,255); // Hover color
            }
        }

        public override void OnPointerExit(PointerEventArgs args)
        {
            base.OnPointerExit(args);
            // Log.Info($"ItemSlot '{this.Entity.Name}': Pointer Exit.");
            parentPanelScript?.HandleSlotPointerExit(this);
            // Example: Restore background if not dragging this slot
            if (!isDragging && RootElement is Panel panel) {
                // panel.BackgroundColor = new Color(64,64,64,255); // Original color
            }
        }
        
        public void OnRightClick(PointerEventArgs args)
        {
            Log.Info($"ItemSlot '{this.Entity.Name}': Right-Clicked.");
            // Placeholder for context menu or other right-click actions
            args.Handled = true; 
        }

        // OnDestroy or Cancel can be used for cleanup if any event handlers were manually subscribed
        // to UI elements, but for overrides, base.Cancel() handles UIScript related cleanup.
        public override void Cancel()
        {
            // Any custom cleanup
            base.Cancel();
        }
    }
}
