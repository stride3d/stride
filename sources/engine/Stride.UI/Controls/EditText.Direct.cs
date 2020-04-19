// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

#if STRIDE_PLATFORM_WINDOWS_DESKTOP || STRIDE_PLATFORM_UNIX
using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Graphics;
using Stride.Input;
using TextAlignment = Stride.Graphics.TextAlignment;

namespace Stride.UI.Controls
{
    public partial class EditText
    {
        private void OnTouchMoveImpl(TouchEventArgs args)
        {
            var currentPosition = FindNearestCharacterIndex(new Vector2(args.WorldPosition.X - WorldMatrix.M41, args.WorldPosition.Y - WorldMatrix.M42));

            if (caretAtStart)
            {
                if (currentPosition < selectionStop)
                    Select(currentPosition, selectionStop - currentPosition, true);
                else
                    Select(selectionStop, currentPosition - selectionStop);  
            }
            else
            {
                if (currentPosition < SelectionStart)
                    Select(currentPosition, selectionStart - currentPosition, true);
                else
                    Select(selectionStart, currentPosition - selectionStart);  
            }
        }

        private void OnTouchDownImpl(TouchEventArgs args)
        {
            // Find the appropriate position for the caret.
            CaretPosition = FindNearestCharacterIndex(new Vector2(args.WorldPosition.X - WorldMatrix.M41, args.WorldPosition.Y - WorldMatrix.M42));
        }
        
        /// <summary>
        /// Find the index of the nearest character to the provided position.
        /// </summary>
        /// <param name="position">The position in edit text space</param>
        /// <returns>The 0-based index of the nearest character</returns>
        protected virtual int FindNearestCharacterIndex(Vector2 position)
        {
            if (Font == null)
                return 0;

            var textRegionSize = (ActualWidth - Padding.Left - Padding.Right);
            var fontScale = LayoutingContext.RealVirtualResolutionRatio;
            var fontSize = new Vector2(fontScale.Y * ActualTextSize); // we don't want letters non-uniform ratio

            // calculate the offset of the beginning of the text due to text alignment
            var alignmentOffset = -textRegionSize / 2f;
            if (TextAlignment != TextAlignment.Left)
            {
                var textWidth = Font.MeasureString(TextToDisplay, ref fontSize).X;
                if (Font.FontType == SpriteFontType.Dynamic)
                    textWidth /= fontScale.X;

                alignmentOffset = TextAlignment == TextAlignment.Center ? -textWidth / 2 : -textRegionSize / 2f + (textRegionSize - textWidth);
            }
            var touchInText = position.X - alignmentOffset;

            // Find the first character starting after the click
            var characterIndex = 1;
            var previousCharacterOffset = 0f;
            var currentCharacterOffset = Font.MeasureString(TextToDisplay, ref fontSize, characterIndex).X;
            while (currentCharacterOffset < touchInText && characterIndex < textToDisplay.Length)
            {
                ++characterIndex;
                previousCharacterOffset = currentCharacterOffset;
                currentCharacterOffset = Font.MeasureString(TextToDisplay, ref fontSize, characterIndex).X;
                if (Font.FontType == SpriteFontType.Dynamic)
                    currentCharacterOffset /= fontScale.X;
            }

            // determine the caret position.
            if (touchInText < 0) // click before the start of the text
            {
                return 0;
            }
            if (currentCharacterOffset < touchInText) // click after the end of the text
            {
                return textToDisplay.Length;
            }

            const float Alpha = 0.66f;
            var previousElementRatio = Math.Abs(touchInText - previousCharacterOffset) / Alpha;
            var currentElementRation = Math.Abs(currentCharacterOffset - touchInText) / (1 - Alpha);
            return previousElementRatio < currentElementRation ? characterIndex - 1 : characterIndex;
        }

        internal override void OnKeyPressed(KeyEventArgs args)
        {
            if (Composition.Length > 0)
                return; // Ignore keys if composing text
            InterpretKey(args.Key, args.Input);
        }

        internal override void OnTextInput(TextEventArgs args)
        {
            if (args.Type == TextInputEventType.Input)
            {
                // Clear the composition first, so it won't be inserted in to the text to display again
                Composition = "";
                SelectedText = args.Text;
            }
            else
            {
                // Update the composition
                Composition = args.Text;
                CompositionStart = args.CompositionStart;
                CompositionLength = args.CompositionLength;
                UpdateTextToDisplay();
                InvalidateMeasure();
            }
        }

        private void ActivateEditTextImpl()
        {
            var input = UIElementServices.Services.GetSafeServiceAs<InputManager>();
            input.TextInput?.EnabledTextInput();
        }
        private void DeactivateEditTextImpl()
        {
            var input = UIElementServices.Services.GetSafeServiceAs<InputManager>();
            input.TextInput?.DisableTextInput();
            Composition = "";

            FocusedElement = null;
        }

        private void InterpretKey(Keys key, InputManager input)
        {
            // delete and back space have same behavior when there is a selection 
            if (SelectionLength > 0 && (key == Keys.Delete || key == Keys.Back))
            {
                SelectedText = "";
                return;
            }

            // backspace with caret 
            if (key == Keys.Back)
            {
                selectionStart = Math.Max(0, selectionStart - 1);
                SelectedText = "";
                return;
            }

            // delete with caret
            if (key == Keys.Delete)
            {
                SelectionLength = 1;
                SelectedText = "";
                return;
            }

            // select until home
            if (key == Keys.Home && (input.IsKeyDown(Keys.LeftShift) || input.IsKeyDown(Keys.RightShift)))
            {
                if (caretAtStart)
                    Select(0, selectionStart + SelectionLength, true);
                else
                    Select(0, selectionStart, true);
                return;
            }

            // select until end
            if (key == Keys.End && (input.IsKeyDown(Keys.LeftShift) || input.IsKeyDown(Keys.RightShift)))
            {
                if (caretAtStart)
                    Select(selectionStop, Text.Length- selectionStop, false);
                else
                    Select(selectionStart, Text.Length - selectionStart, false);
                return;
            }

            // move to home
            if (key == Keys.Home)
            {
                CaretPosition = 0;
                return;
            }

            // move to end
            if (key == Keys.End)
            {
                CaretPosition = Text.Length;
                return;
            }

            // select backward 
            if (key == Keys.Left && (input.IsKeyDown(Keys.LeftShift) || input.IsKeyDown(Keys.RightShift)))
            {
                if (caretAtStart || selectionStart == selectionStop)
                {
                    if (selectionStart > 0)
                        Select(selectionStart - 1, SelectionLength + 1, true);
                }
                else
                    Select(selectionStart, SelectionLength - 1);

                return;
            }

            // select forward
            if (key == Keys.Right && (input.IsKeyDown(Keys.LeftShift) || input.IsKeyDown(Keys.RightShift)))
            {
                if (caretAtStart && selectionStart != selectionStop)
                    Select(selectionStart + 1, SelectionLength - 1, true);
                else
                    Select(selectionStart, SelectionLength + 1);

                return;
            }

            // move backward
            if (key == Keys.Left)
            {
                CaretPosition = CaretPosition - 1;
                return;
            }

            // move forward
            if (key == Keys.Right)
            {
                CaretPosition = CaretPosition + 1;
                return;
            }

            // validate the text with "enter" or "escape"
            if (key == Keys.Enter || key == Keys.Escape || key == Keys.NumPadEnter)
            {
                IsSelectionActive = false;
                return;
            }
        }
    }
}

#endif
