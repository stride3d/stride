// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Engine;
using Stride.Graphics; // For Texture
using Stride.UI;
using Stride.UI.Controls; // For Image, TextBlock, Button, Border
using Stride.UI.Events;   // For RoutedEventArgs
using Stride.Core.Mathematics; // For Color
using System; // For Action
using FirstPersonShooter.Items.Engrams; // For EngramEntry, EngramStatus

namespace FirstPersonShooter.UI.Scripts
{
    public class EngramNodeScript : UIScript
    {
        public EngramEntry CurrentEngram { get; private set; }

        private ImageElement engramIconImage;
        private TextBlock engramNameText;
        private Button engramNodeButton;
        private Border statusIndicator; // Optional status indicator
        
        private Action<EngramEntry> currentOnClickAction;

        public override void Start()
        {
            base.Start();

            var rootElement = Entity.Get<UIComponent>()?.Page?.RootElement;
            if (rootElement == null)
            {
                Log.Error("EngramNodeScript: Root UI element not found.");
                return;
            }

            engramIconImage = rootElement.FindName<ImageElement>("EngramIconImage");
            engramNameText = rootElement.FindName<TextBlock>("EngramNameText");
            engramNodeButton = rootElement as Button; // The root element itself is the Button
            statusIndicator = rootElement.FindName<Border>("StatusIndicator"); // Optional

            if (engramIconImage == null) Log.Error("EngramNodeScript: EngramIconImage not found.");
            if (engramNameText == null) Log.Error("EngramNodeScript: EngramNameText not found.");
            if (engramNodeButton == null) Log.Error("EngramNodeScript: EngramNodeButton (root) not found or is not a Button.");
            // No error for statusIndicator as it's optional
        }

        public void Initialize(EngramEntry engram, Action<EngramEntry> onClickAction)
        {
            CurrentEngram = engram;

            if (engramNameText != null)
            {
                engramNameText.Text = engram?.DisplayName ?? "N/A";
            }

            if (engramIconImage != null)
            {
                if (engram?.Icon != null)
                {
                    engramIconImage.Source = new SpriteFromTexture(engram.Icon);
                    engramIconImage.Visibility = Visibility.Visible;
                }
                else
                {
                    engramIconImage.Source = null; // Or a default placeholder icon
                    engramIconImage.Visibility = Visibility.Hidden;
                }
            }
            
            UpdateStatusVisuals(); // Update visuals based on initial status

            if (engramNodeButton != null)
            {
                // Remove previous handler if any
                if (currentOnClickAction != null)
                {
                    engramNodeButton.Click -= OnEngramNodeButtonClick;
                }
                
                currentOnClickAction = onClickAction;
                if (currentOnClickAction != null)
                {
                    engramNodeButton.Click += OnEngramNodeButtonClick;
                }
            }
        }

        private void OnEngramNodeButtonClick(object sender, RoutedEventArgs e)
        {
            currentOnClickAction?.Invoke(CurrentEngram);
        }

        public void UpdateStatusVisuals()
        {
            if (CurrentEngram == null || engramNodeButton == null) return;

            // Example: Change background color or border based on status
            // These colors are placeholders; define proper style in XAML or use style sheets.
            Color statusColor = Colors.DarkGray; // Default for Locked
            switch (CurrentEngram.Status)
            {
                case EngramStatus.Unlockable:
                    statusColor = Colors.YellowGreen; // Or some other distinct color
                    engramNodeButton.IsEnabled = true;
                    break;
                case EngramStatus.Unlocked:
                    statusColor = Colors.ForestGreen; // Or a "learned" color
                    engramNodeButton.IsEnabled = false; // Already unlocked, maybe not clickable or different action
                    break;
                case EngramStatus.Locked:
                default:
                    statusColor = Colors.DimGray; // Locked color
                    engramNodeButton.IsEnabled = false; // Cannot unlock yet
                    break;
            }

            if (statusIndicator != null)
            {
                statusIndicator.Background = new SolidColorBrush(statusColor);
            }
            else if (engramNodeButton != null) // Fallback to button background if no indicator
            {
                 // engramNodeButton.BackgroundColor = statusColor; // This might override XAML styles too much
                 // Better to use a dedicated status indicator element or style states.
                 // For now, let's just change opacity for locked/unlocked.
                 engramNodeButton.Opacity = (CurrentEngram.Status == EngramStatus.Locked) ? 0.6f : 1.0f;
            }
            
            // Potentially update text color or other visual cues
            if (engramNameText != null)
            {
                engramNameText.TextColor = (CurrentEngram.Status == EngramStatus.Locked) ? Colors.LightSlateGray : Colors.White;
            }
        }
        
        public override void Cancel()
        {
            if (engramNodeButton != null && currentOnClickAction != null)
            {
                engramNodeButton.Click -= OnEngramNodeButtonClick;
            }
            currentOnClickAction = null;
            base.Cancel();
        }
    }
}
