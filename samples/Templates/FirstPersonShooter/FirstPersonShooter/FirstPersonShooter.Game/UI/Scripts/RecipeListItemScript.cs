// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using Stride.Graphics; // For Texture
using Stride.UI;
using Stride.UI.Controls; // For Image, TextBlock, Button
using Stride.UI.Events;   // For RoutedEventArgs
using System; // For Action
using FirstPersonShooter.Items.Crafting; // For CraftingRecipe

namespace FirstPersonShooter.UI.Scripts
{
    public class RecipeListItemScript : UIScript
    {
        public CraftingRecipe CurrentRecipe { get; private set; }

        private ImageElement itemIconImage;
        private TextBlock itemNameText;
        private Button recipeButton;
        
        private Action<CraftingRecipe> currentOnClickAction; // Store to remove later

        public override void Start()
        {
            base.Start();

            var rootElement = Entity.Get<UIComponent>()?.Page?.RootElement;
            if (rootElement == null)
            {
                Log.Error("RecipeListItemScript: Root UI element not found.");
                return;
            }

            itemIconImage = rootElement.FindName<ImageElement>("ItemIconImage");
            itemNameText = rootElement.FindName<TextBlock>("ItemNameText");
            // The root element itself is the Button in RecipeListItem.sdslui
            recipeButton = rootElement as Button; 

            if (itemIconImage == null) Log.Error("RecipeListItemScript: ItemIconImage not found.");
            if (itemNameText == null) Log.Error("RecipeListItemScript: ItemNameText not found.");
            if (recipeButton == null) Log.Error("RecipeListItemScript: RecipeButton (root element) not found or is not a Button.");
        }

        public void Initialize(CraftingRecipe recipe, Action<CraftingRecipe> onClickAction)
        {
            CurrentRecipe = recipe;

            if (itemNameText != null)
            {
                itemNameText.Text = recipe?.DisplayName ?? "Unnamed Recipe";
            }

            if (itemIconImage != null)
            {
                if (recipe?.Icon != null)
                {
                    itemIconImage.Source = new SpriteFromTexture(recipe.Icon);
                    itemIconImage.Visibility = Visibility.Visible;
                }
                else
                {
                    itemIconImage.Source = null; // Or a default placeholder icon
                    itemIconImage.Visibility = Visibility.Collapsed; // Or Hidden
                }
            }

            if (recipeButton != null)
            {
                // Remove previous handler if any
                if (currentOnClickAction != null)
                {
                    recipeButton.Click -= OnRecipeButtonClick;
                }
                
                // Store and subscribe new action
                currentOnClickAction = onClickAction;
                if (currentOnClickAction != null)
                {
                    recipeButton.Click += OnRecipeButtonClick;
                }
            }
        }

        private void OnRecipeButtonClick(object sender, RoutedEventArgs e)
        {
            currentOnClickAction?.Invoke(CurrentRecipe);
        }
        
        public override void Cancel()
        {
            // Cleanup: Unsubscribe from events
            if (recipeButton != null && currentOnClickAction != null)
            {
                recipeButton.Click -= OnRecipeButtonClick;
            }
            currentOnClickAction = null;
            base.Cancel();
        }
    }
}
