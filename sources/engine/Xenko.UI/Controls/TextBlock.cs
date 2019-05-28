// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.ComponentModel;
using System.Diagnostics;
using System.Text;

using Xenko.Core;
using Xenko.Core.Annotations;
using Xenko.Core.Mathematics;
using Xenko.Graphics;

namespace Xenko.UI.Controls
{
    /// <summary>
    /// Provides a lightweight control for displaying small amounts of text.
    /// </summary>
    [DataContract(nameof(TextBlock))]
    [DebuggerDisplay("TextBlock - Name={Name}")]
    public class TextBlock : UIElement
    {
        private SpriteFont font;
        private string text;
        private float textSize = float.NaN;
        private bool wrapText;
        private bool synchronousCharacterGeneration;

        private string wrappedText;

        /// <summary>
        /// Returns the actual size of the text in virtual pixels unit.
        /// </summary>
        /// <remarks>If <see cref="TextSize"/> is <see cref="float.IsNaN"/>, returns the default size of the <see cref="Font"/>.</remarks>
        /// <seealso cref="TextSize"/>
        /// <seealso cref="SpriteFont.Size"/>
        public float ActualTextSize => !float.IsNaN(TextSize) ? TextSize : Font?.Size ?? 0;

        /// <summary>
        /// Returns the text to display during the draw call.
        /// </summary>
        public virtual string TextToDisplay => WrapText ? wrappedText : Text;

        /// <summary>
        /// Gets or sets the text of the text block.
        /// </summary>
        /// <userdoc>The text of the text block.</userdoc>
        [DataMember]
        [DefaultValue(null)]
        public string Text
        {
            get { return text; }
            set
            {
                if (text == value) return;
                text = value;
                OnTextChanged();
            }
        }

        /// <summary>
        /// Gets or sets the font of the text block.
        /// </summary>
        /// <userdoc>The font of the text block.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public SpriteFont Font
        {
            get { return font; }
            set
            {
                if (font == value)
                    return;

                font = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets or sets the size of the text in virtual pixels unit.
        /// </summary>
        /// <remarks>If the value set is <c>null</c>, the default size of the <see cref="Font"/> will be used instead.</remarks>
        /// <seealso cref="ActualTextSize"/>
        /// <seealso cref="SpriteFont.Size"/>
        /// <userdoc>The size of the text in virtual pixels unit.</userdoc>
        [DataMember]
        [DataMemberRange(0.0f, 3)]
        [Display(category: AppearanceCategory)]
        [DefaultValue(float.NaN)]
        public float TextSize
        {
            get { return textSize; }
            set
            {
                float clamped = MathUtil.Clamp(value, 0.0f, float.MaxValue);
                if (textSize == clamped) return;
                textSize = clamped;
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets or sets the color of the text.
        /// </summary>
        /// <userdoc>The color of the text.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        public Color TextColor { get; set; } = Color.FromAbgr(0xF0F0F0FF);

        /// <summary>
        /// Gets or sets the alignment of the text to display.
        /// </summary>
        /// <userdoc>Alignment of the text.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(default(TextAlignment))]
        public TextAlignment TextAlignment { get; set; }

        /// <summary>
        /// Gets or sets the value indicating if the <see cref="Text"/> of the <see cref="TextBlock"/> 
        /// should automatically return to the beginning of the line when it is too long for the line width.
        /// </summary>
        /// <userdoc>True if the text should automatically return of the beginning of the line when it is too long to fit the line width, false otherwise.</userdoc>
        [DataMember]
        [Display(category: BehaviorCategory)]
        [DefaultValue(false)]
        public bool WrapText
        {
            get { return wrapText; }
            set
            {
                if (wrapText == value)
                    return;

                wrapText = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets or sets the value indicating if the snapping of the <see cref="Text"/> of the <see cref="TextBlock"/> to the closest screen pixel should be skipped.
        /// </summary>
        /// <remarks>
        /// When <value>true</value>, the element's text is never snapped. 
        /// When <value>false</value>, it is snapped only if the font is dynamic and the element is rendered by a SceneUIRenderer.
        /// </remarks>
        /// <userdoc>True to never snap to the closest screen pixel, false to snap it (only works for dynamic font).</userdoc>
        [DataMember]
        [Display(category: BehaviorCategory)]
        [DefaultValue(false)]
        public bool DoNotSnapText { get; set; }

        /// <summary>
        /// Gets or sets the value indicating if the text block should generate <see cref="Graphics.Font.RuntimeRasterizedSpriteFont"/> characters synchronously or asynchronously.
        /// </summary>
        /// <remarks>
        /// If synchronous generation is activated, the game will be block until all the characters have finished to be generate.
        /// If asynchronous generation is activated, some characters can appears with one or two frames of delay.
        /// </remarks>
        /// <userdoc>True if dynamic characters should be generated synchronously, false otherwise.</userdoc>
        [DataMember]
        [Display(category: BehaviorCategory)]
        [DefaultValue(false)]
        public bool SynchronousCharacterGeneration
        {
            get { return synchronousCharacterGeneration; }
            set
            {
                if (synchronousCharacterGeneration == value)
                    return;

                synchronousCharacterGeneration = value;

                if (IsMeasureValid && synchronousCharacterGeneration)
                    CalculateTextSize();
            }
        }

        /// <summary>
        /// Calculate and returns the size of the <see cref="Text"/> in virtual pixels size.
        /// </summary>
        /// <returns>The size of the Text in virtual pixels.</returns>
        public Vector2 CalculateTextSize()
        {
            return CalculateTextSize(TextToDisplay);
        }

        /// <inheritdoc/>
        protected override Vector3 ArrangeOverride(Vector3 finalSizeWithoutMargins)
        {
            if (WrapText)
                UpdateWrappedText(finalSizeWithoutMargins);

            return base.ArrangeOverride(finalSizeWithoutMargins);
        }

        /// <summary>
        /// Calculate and returns the size of the provided <paramref name="textToMeasure"/>"/> in virtual pixels size.
        /// </summary>
        /// <param name="textToMeasure">The text to measure</param>
        /// <returns>The size of the text in virtual pixels</returns>
        protected Vector2 CalculateTextSize(string textToMeasure)
        {
            if (textToMeasure == null)
                return Vector2.Zero;

            return CalculateTextSize(new SpriteFont.StringProxy(textToMeasure));
        }

        /// <inheritdoc/>
        protected override Vector3 MeasureOverride(Vector3 availableSizeWithoutMargins)
        {
            if (WrapText)
                UpdateWrappedText(availableSizeWithoutMargins);

            return new Vector3(CalculateTextSize(), 0);
        }

        /// <summary>
        /// Method triggered when the <see cref="Text"/> changes.
        /// Can be overridden in inherited class to changed the default behavior.
        /// </summary>
        protected virtual void OnTextChanged()
        {
            InvalidateMeasure();
        }

        private Vector2 CalculateTextSize(StringBuilder textToMeasure)
        {
            return CalculateTextSize(new SpriteFont.StringProxy(textToMeasure));
        }

        private Vector2 CalculateTextSize(SpriteFont.StringProxy textToMeasure)
        {
            if (Font == null)
                return Vector2.Zero;

            var sizeRatio = LayoutingContext.RealVirtualResolutionRatio;
            var measureFontSize = new Vector2(sizeRatio.Y * ActualTextSize); // we don't want letters non-uniform ratio
            var realSize = Font.MeasureString(ref textToMeasure, ref measureFontSize);

            // force pre-generation if synchronous generation is required
            if (SynchronousCharacterGeneration)
                Font.PreGenerateGlyphs(ref textToMeasure, ref measureFontSize);

            if (Font.FontType == SpriteFontType.Dynamic)
            {
                // rescale the real size to the virtual size
                realSize.X /= sizeRatio.X;
                realSize.Y /= sizeRatio.Y;
            }

            if (Font.FontType == SpriteFontType.SDF)
            {
                var scaleRatio = ActualTextSize / Font.Size;
                realSize.X *= scaleRatio;
                realSize.Y *= scaleRatio;
            }

            return realSize;
        }

        private void UpdateWrappedText(Vector3 availableSpace)
        {
            if (string.IsNullOrEmpty(text))
            {
                wrappedText = string.Empty;

                return;
            }

            var availableWidth = availableSpace.X;
            var currentLine = new StringBuilder(text.Length);
            var currentText = new StringBuilder(2 * text.Length);

            var indexOfNewLine = 0;
            while (true)
            {
                float lineCurrentSize;
                var indexNextCharacter = 0;
                var indexOfLastSpace = -1;

                while (true)
                {
                    lineCurrentSize = CalculateTextSize(currentLine).X;

                    if (lineCurrentSize > availableWidth || indexOfNewLine + indexNextCharacter >= text.Length)
                        break;

                    var currentCharacter = text[indexOfNewLine + indexNextCharacter];

                    if (currentCharacter == '\n')
                    {
                        indexOfNewLine += indexNextCharacter + 1;
                        goto AppendLine;
                    }

                    currentLine.Append(currentCharacter);

                    if (char.IsWhiteSpace(currentCharacter))
                        indexOfLastSpace = indexNextCharacter;

                    ++indexNextCharacter;

                }

                if (lineCurrentSize <= availableWidth) // we reached the end of the text.
                {
                    // append the final part of the text and quit the main loop
                    currentText.Append(currentLine);
                    break;
                }

                // we reached the end of the line.
                if (indexOfLastSpace < 0) // no space in the line
                {
                    // remove last extra character
                    currentLine.Remove(currentLine.Length - 1, 1);
                    indexOfNewLine += indexNextCharacter - 1;
                }
                else // at least one white space in the line
                {
                    // remove all extra characters until last space (included)
                    if (indexNextCharacter > indexOfLastSpace)
                        currentLine.Remove(indexOfLastSpace, indexNextCharacter - indexOfLastSpace);
                    indexOfNewLine += indexOfLastSpace + 1;
                }

                AppendLine:

                // add the next line to the current text
                currentLine.Append('\n');
                currentText.Append(currentLine);

                // reset current line
                currentLine.Clear();
            }

            wrappedText = currentText.ToString();
        }
    }
}
