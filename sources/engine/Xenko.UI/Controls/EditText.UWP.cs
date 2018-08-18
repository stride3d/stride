// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if XENKO_PLATFORM_UWP
using System;
using Windows.UI;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Media;
using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Games;

namespace Xenko.UI.Controls
{
    // Note: this completes EditText.Direct.cs
    public partial class EditText
    {
        private static EditText activeEditText;
        private Windows.UI.Xaml.Controls.TextBox editText;
        private GameContextUWPXaml gameContext;

        private static void InitializeStaticImpl()
        {
        }

        private void InitializeImpl()
        {
        }

        private int GetLineCountImpl()
        {
            if (Font == null)
                return 1;

            return text.Split('\n').Length;
        }

        private void OnMaxLinesChangedImpl()
        {
        }

        private void OnMinLinesChangedImpl()
        {
        }

        private void ActivateEditTextImpl()
        {
            // try to show the virtual keyboard if no hardward keyboard available
            Windows.UI.ViewManagement.InputPane.GetForCurrentView().TryShow();
            
            var game = GetGame();

            // Detach previous EditText (if any)
            if (activeEditText != null)
                activeEditText.IsSelectionActive = false;
            activeEditText = this;

            // Handle only GameContextUWPXaml for now
            // TODO: Implement EditText for GameContextUWPCoreWindow
            gameContext = game.Context as GameContextUWPXaml;
            if (gameContext == null)
                return;

            var swapChainPanel = gameContext.Control;

            // Make sure it doesn't have a parent (another text box being edited)
            editText = gameContext.EditTextBox;
            editText.Text = text;
            swapChainPanel.Children.Add(new Windows.UI.Xaml.Controls.Grid { Children = { editText }, Opacity = 0.7f, Background = new SolidColorBrush(Colors.Black)});

            editText.TextChanged += EditText_TextChanged;
            editText.KeyDown += EditText_KeyDown;
        }

        private void EditText_KeyDown(object sender, Windows.UI.Xaml.Input.KeyRoutedEventArgs e)
        {
            // validate the text with "enter" or "escape"
            if (e.Key == Windows.System.VirtualKey.Enter || e.Key == Windows.System.VirtualKey.Escape)
            {
                IsSelectionActive = false;
                e.Handled = true;
            }
        }

        private void EditText_TextChanged(object sender, Windows.UI.Xaml.Controls.TextChangedEventArgs e)
        {
            if (editText == null)
                return;

            // early exit if text did not changed 
            if (text == editText.Text)
                return;

            // Make sure selection is not reset
            var editSelectionStart = editText.SelectionStart;

            SetTextInternal(editText.Text, false);
            UpdateTextToEditImpl();

            if (editSelectionStart >= text.Length)
                editSelectionStart = text.Length;
            editText.SelectionStart = editSelectionStart;

            UpdateSelectionFromEditImpl();
        }

        private void DeactivateEditTextImpl()
        {
            if (editText != null)
            {
                // Remove text box
                editText.TextChanged -= EditText_TextChanged;
                editText.KeyDown -= EditText_KeyDown;
                var stackPanel = (Windows.UI.Xaml.Controls.Panel)editText.Parent;
                stackPanel.Children.Remove(editText);

                var swapChainControl = gameContext?.Control;
                swapChainControl?.Children.Remove(stackPanel);

                editText = null;
                activeEditText = null;
            }
            FocusedElement = null;
        }

        private void UpdateTextToEditImpl()
        {
            if (editText == null)
                return;

            if (editText.Text != Text) // avoid infinite text changed triggering loop.
            {
                editText.Text = text;
            }
        }

        private void UpdateInputTypeImpl()
        {
        }

        private void UpdateSelectionFromEditImpl()
        {
            if (editText == null)
                return;

            selectionStart = editText.SelectionStart;
            selectionStop = editText.SelectionStart + editText.SelectionLength;
        }

        private void UpdateSelectionToEditImpl()
        {
            if (editText == null)
                return;

            editText.Select(selectionStart, selectionStop - selectionStart);
        }

        private void OnTouchDownImpl(TouchEventArgs args)
        {
        }

        private void OnTouchMoveImpl(TouchEventArgs args)
        {
        }

        private void OnTouchUpImpl(TouchEventArgs args)
        {
            if (editText.FocusState == FocusState.Unfocused)
                editText.Focus(FocusState.Programmatic);
        }

        [NotNull]
        private IGame GetGame()
        {
            if (UIElementServices.Services == null)
                throw new InvalidOperationException("services");

            return UIElementServices.Services.GetSafeServiceAs<IGame>();
        }
    }
}
#endif
