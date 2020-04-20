// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
#if STRIDE_PLATFORM_IOS

using System;
using System.Diagnostics;
using System.Drawing;
using Foundation;
using UIKit;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Games;
using Stride.UI.Events;

namespace Stride.UI.Controls
{
    public partial class EditText
    {
        private UITextField attachedTextField;

        private static UIButton doneButton;
        private static UITextField textField;
        private static EditText currentActiveEditText;
        private static UIView barView;
        private static UIView overlayView;
        private static GameContextiOS gameContext;

        private static void InitializeStaticImpl()
        {
            doneButton = UIButton.FromType(UIButtonType.RoundedRect);
            doneButton.SetTitle(NSBundle.MainBundle.LocalizedString("UIDoneButton", null), UIControlState.Normal);
            doneButton.SetTitleColor(UIColor.Black, UIControlState.Normal);
            doneButton.TouchDown += DoneButtonOnTouchDown;

            textField = new UITextField
            {
                KeyboardType = UIKeyboardType.Default,
                BorderStyle = UITextBorderStyle.RoundedRect,
            };
            textField.EditingDidEnd += TextFieldOnEditingDidEnd;
            textField.EditingDidBegin += TextFieldOnEditingDidBegin;

            barView = new UIView { Hidden = true };
            barView.AddSubview(textField);
            barView.AddSubview(doneButton);
            barView.BackgroundColor = UIColor.Gray;

            overlayView = new UIView { Hidden = true };
            overlayView.AddSubview(barView);
            overlayView.BackgroundColor = new UIColor(0,0,0,0.4f);
        }

        [NotNull]
        private GameBase GetGame()
        {
            if (UIElementServices.Services == null)
                throw new InvalidOperationException("services");

            return (GameBase)UIElementServices.Services.GetSafeServiceAs<IGame>();
        }

        private static void TextFieldOnEditingDidBegin(object sender, EventArgs eventArgs)
        {
            overlayView.Hidden = false;
            barView.Hidden = false;

            if (currentActiveEditText != null)
            {
                // TODO: Check if this is still needed; currently disabled since SlowDownDrawCalls was removed
                // we need to skip some draw calls here to let the time to iOS to draw its own keyboard animations... (Thank you iOS)
                // If we don't do this when changing the type of keyboard (split / docked / undocked), the keyboard freeze for about 5/10 seconds before updating.
                // Note: Setting UIView.EnableAnimation to false does not solve the problem. Only animation when the keyboard appear/disappear are skipped.
                //currentActiveEditText.GetGame().SlowDownDrawCalls = true;
            }
        }

        private void InitializeImpl()
        {
            // nothing to do here
        }

        private void EnsureGameContext()
        {
            if (gameContext == null)
            {
                var game = GetGame();

                Debug.Assert(game.Context is GameContextiOS, "There is only one possible descendant of GameContext for iOS.");

                gameContext = (GameContextiOS)game.Context;
                gameContext.Control.GameView.AddSubview(overlayView);

                NSNotificationCenter.DefaultCenter.AddObserver(UIDevice.OrientationDidChangeNotification, OnScreenRotated);

                UpdateOverlayAndEditBarLayout();
            }
        }

        private void OnScreenRotated(NSNotification nsNotification)
        {
            if (gameContext == null)
                return;

            UpdateOverlayAndEditBarLayout();
        }

        private static void UpdateOverlayAndEditBarLayout()
        {
            const int spaceX = 10;
            const int spaceY = 5;
            const int buttonWidth = 60;
            const int buttonHeight = 35;
            const int barHeight = buttonHeight + 2*spaceY;

            var viewFrame = gameContext.Control.GameView.Frame;

            barView.Frame = new RectangleF(0, 0, (int)viewFrame.Width, barHeight);
            overlayView.Frame = new RectangleF((int)viewFrame.X, (int)viewFrame.Y, 2 * (int)viewFrame.Width, (int)viewFrame.Height); // if we don't over-set width background can be seen during rotation...
            textField.Frame = new RectangleF(spaceX, spaceY, (int)viewFrame.Width - buttonWidth - 3 * spaceX, buttonHeight);
            doneButton.Frame = new RectangleF((int)viewFrame.Width - buttonWidth - spaceX, spaceY, buttonWidth, buttonHeight);
        }

        private static void TextFieldOnEditingDidEnd(object sender, EventArgs eventArgs)
        {
            currentActiveEditText.IsSelectionActive = false;
            barView.Hidden = true;
            overlayView.Hidden = true;
            FocusedElement = null;

            if (currentActiveEditText != null)
            {
                // TODO: Check if this is still needed; currently disabled since SlowDownDrawCalls was removed
                // Editing finished, we can now draw back to normal frame rate.
                //currentActiveEditText.GetGame().SlowDownDrawCalls = false;
            }
        }

        private static void DoneButtonOnTouchDown(object sender, EventArgs eventArgs)
        {
            currentActiveEditText.IsSelectionActive = false;
        }

        private void TextFieldOnValueChanged(object sender, EventArgs eventArgs)
        {
            if (attachedTextField == null)
                return;

            // early exit if text did not changed
            if (text == attachedTextField.Text)
                return;

            text = attachedTextField.Text;
            UpdateTextToDisplay();

            RaiseEvent(new RoutedEventArgs(TextChangedEvent));
            InvalidateMeasure();
        }

        private int GetLineCountImpl()
        {
            return 1;
        }

        private void OnMaxLinesChangedImpl()
        {
        }

        private void OnMinLinesChangedImpl()
        {
        }

        private void ActivateEditTextImpl()
        {
            EnsureGameContext();

            currentActiveEditText = this;
            attachedTextField = textField;

            UpdateInputTypeImpl();
            attachedTextField.Text = text;
            attachedTextField.EditingChanged += TextFieldOnValueChanged;
            attachedTextField.ShouldChangeCharacters += ShouldChangeCharacters;
            attachedTextField.BecomeFirstResponder();
        }

        private bool ShouldChangeCharacters(UITextField theTextField, NSRange range, string replacementString)
        {
            // check that new characters are correct.
            var predicate = CharacterFilterPredicate;
            foreach (var character in replacementString)
            {
                if (predicate != null && !predicate(character))
                    return false;
            }

            var replacementSize = replacementString.Length - range.Length;
            return replacementSize < 0 || theTextField.Text.Length + replacementSize <= MaxLength;
        }

        private void DeactivateEditTextImpl()
        {
            attachedTextField.EditingChanged -= TextFieldOnValueChanged;
            attachedTextField.ShouldChangeCharacters -= ShouldChangeCharacters;
            attachedTextField.SecureTextEntry = false;
            attachedTextField.ResignFirstResponder();
            attachedTextField = null;
            currentActiveEditText = null;
        }

        private void OnTouchMoveImpl(TouchEventArgs args)
        {
        }

        private void OnTouchDownImpl(TouchEventArgs args)
        {
        }

        private void OnTouchUpImpl(TouchEventArgs args)
        {
        }

        private void UpdateInputTypeImpl()
        {
            if (attachedTextField == null)
                return;

            attachedTextField.SecureTextEntry = ShouldHideText;
        }

        private void UpdateSelectionToEditImpl()
        {
            if (attachedTextField == null)
                return;

            attachedTextField.SelectedTextRange = attachedTextField.GetTextRange(
                attachedTextField.GetPosition(attachedTextField.BeginningOfDocument, selectionStart),
                attachedTextField.GetPosition(attachedTextField.BeginningOfDocument, selectionStop));
        }

        private void UpdateSelectionFromEditImpl()
        {
            if (attachedTextField == null)
                return;

            selectionStart = (int)attachedTextField.GetOffsetFromPosition(attachedTextField.BeginningOfDocument, attachedTextField.SelectedTextRange.Start);
            selectionStop = (int)attachedTextField.GetOffsetFromPosition(attachedTextField.BeginningOfDocument, attachedTextField.SelectedTextRange.End);
        }

        private void UpdateTextToEditImpl()
        {
            if (attachedTextField == null)
                return;

            // update the iOS text edit only the text changed to avoid re-triggering events.
            if (Text != attachedTextField.Text)
                attachedTextField.Text = text;
        }
    }
}

#endif
