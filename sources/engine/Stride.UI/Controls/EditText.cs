// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Games;
using Stride.Graphics;
using Stride.UI.Events;

namespace Stride.UI.Controls
{
    /// <summary>
    /// Represent an edit text where the user can enter text.
    /// </summary>
    [DataContract(nameof(EditText))]
    [DebuggerDisplay("EditText - Name={Name}")]
    [Display(category: InputCategory)]
    public partial class EditText : Control
    {
        private float textSize = float.NaN;

        private InputTypeFlags inputType;

        private const char PasswordHidingCharacter = '*';

        private string text = "";
        private string textToDisplay = "";

        private int selectionStart;
        private int selectionStop;
        private bool caretAtStart;

        private float caretWith;
        private float caretFrequency;
        private bool caretHidden;
        private float accumulatedTime;

        private bool synchronousCharacterGeneration;

        private readonly StringBuilder builder = new StringBuilder();

        [Flags]
        public enum InputTypeFlags
        {
            /// <summary>
            /// No specific input type for the <see cref="EditText"/>.
            /// </summary>
            /// <userdoc>No specific input type for the Edit Text.</userdoc>
            None,

            /// <summary>
            /// A password input type. Password text is hidden while editing.
            /// </summary>
            /// <userdoc>A password input type. Password text is hidden while editing.</userdoc>
            Password,
        }

        /// <summary>
        /// Function triggered when the value of <see cref="IsReadOnly"/> changed.
        /// </summary>
        protected virtual void OnIsReadOnlyChanged()
        {
            IsSelectionActive = false;
        }

        /// <summary>
        /// Function triggered when the value of <see cref="MaxLength"/> changed.
        /// </summary>
        protected virtual void OnMaxLengthChanged()
        {
            var previousCaret = CaretPosition;
            var previousSelectionStart = SelectionStart;
            var previousSelectionLength = SelectionLength;

            Text = text;

            CaretPosition = previousCaret;
            Select(previousSelectionStart, previousSelectionLength);
        }

        /// <summary>
        /// Function triggered when the value of <see cref="MaxLines"/> changed.
        /// </summary>
        protected virtual void OnMaxLinesChanged()
        {
            OnMaxLinesChangedImpl();
            InvalidateMeasure();
        }

        /// <summary>
        /// Function triggered when the value of <see cref="MinLines"/> changed.
        /// </summary>
        protected virtual void OnMinLinesChanged()
        {
            OnMinLinesChangedImpl();
            InvalidateMeasure();
        }

        static EditText()
        {
            EventManager.RegisterClassHandler(typeof(EditText), TextChangedEvent, TextChangedClassHandler);

            InitializeStaticImpl();
        }

        /// <summary>
        /// Create a new instance of <see cref="EditText"/>.
        /// </summary>
        public EditText()
        {
            InitializeImpl();

            CanBeHitByUser = true;
            IsSelectionActive = false;
            Padding = new Thickness(8, 4, 0, 8, 8, 0);
            DrawLayerNumber += 4; // ( 1: image, 2: selection, 3: Text, 4:Cursor) 
            CaretWidth = 1f;
            CaretFrequency = 1f;
        }

        private bool isSelectionActive;

        private Func<char, bool> characterFilterPredicate;
        private int minLines = 1;
        private int maxLines = int.MaxValue;
        private int maxLength = int.MaxValue;
        private SpriteFont font;
        private bool isReadOnly;

        /// <summary>
        /// Gets a value that indicates whether the text box has focus and selected text.
        /// </summary>
        [DataMemberIgnore]
        public bool IsSelectionActive
        {
            get { return isSelectionActive; }
            set
            {
                if (isSelectionActive == value)
                    return;

                if (IsReadOnly && value) // prevent selection when the Edit is read only
                    return;

                isSelectionActive = value;

                if (IsSelectionActive)
                {
                    var previousEditText = FocusedElement as EditText;
                    if (previousEditText != null)
                        previousEditText.IsSelectionActive = false;

                    FocusedElement = this;
                    ActivateEditTextImpl();
                }
                else
                {
                    DeactivateEditTextImpl();
                }
            }
        }

        public override bool IsEnabled
        {
            set
            {
                if (!value && IsSelectionActive)
                    IsSelectionActive = false;

                base.IsEnabled = value;
            }
        }

        /// <summary>
        /// Gets or sets the value indicating if the text block should generate <see cref="Graphics.Font.RuntimeRasterizedSpriteFont"/> characters synchronously or asynchronously.
        /// </summary>
        /// <remarks>If synchronous generation is activated, the game will be block until all the characters have finished to be generate.
        /// If asynchronous generation is activated, some characters can appears with one or two frames of delay.</remarks>
        /// <userdoc>True if dynamic characters should be generated synchronously, false otherwise.</userdoc>
        [DataMember]
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
        /// Gets a value indicating if the text should be hidden when displayed.
        /// </summary>
        protected bool ShouldHideText => (inputType & InputTypeFlags.Password) != 0;

        /// <summary>
        /// Gets or sets the font of the text block.
        /// </summary>
        /// <userdoc>The font of the text block.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
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
        /// <remarks>The value is coerced in the range [0, <see cref="float.MaxValue"/>].</remarks>
        /// <userdoc>The size of the text in virtual pixels unit.</userdoc>
        [DataMember]
        [DataMemberRange(0, 2)]
        [Display(category: AppearanceCategory)]
        [DefaultValue(float.NaN)]
        public float TextSize
        {
            get { return textSize; }
            set
            {
                textSize = MathUtil.Clamp(value, 0.0f, float.MaxValue);
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
        /// <userdoc>The alignment of the text to display.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(default(TextAlignment))]
        public TextAlignment TextAlignment { get; set; }

        /// <summary>
        /// Gets or sets the image that is displayed in background when the edit is active.
        /// </summary>
        /// <userdoc>The image that is displayed in background when the edit is active.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public ISpriteProvider ActiveImage { get; set; }

        /// <summary>
        /// Gets or sets the image that is displayed in background when the edit is inactive.
        /// </summary>
        /// <userdoc>The image that is displayed in background when the edit is inactive.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public ISpriteProvider InactiveImage { get; set; }

        /// <summary>
        /// Gets or sets the image that the button displays when the mouse is over it.
        /// </summary>
        /// <userdoc>The image that the button displays when the mouse is over it.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public ISpriteProvider MouseOverImage { get; set; }

        /// <summary>
        /// Gets or sets the color of the caret.
        /// </summary>
        /// <userdoc>The color of the caret.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        public Color CaretColor { get; set; } = Color.FromAbgr(0xF0F0F0FF);

        /// <summary>
        /// Gets or sets the width of the edit text's cursor (in virtual pixels).
        /// </summary>
        /// <remarks>The value is coerced in the range [0, <see cref="float.MaxValue"/>].</remarks>
        /// <userdoc>The width of the edit text's cursor (in virtual pixels).</userdoc>
        [DataMember]
        [DataMemberRange(0, 2)]
        [Display(category: AppearanceCategory)]
        [DefaultValue(0)]
        public float CaretWidth
        {
            get { return caretWith; }
            set
            {
                if (float.IsNaN(value))
                    return;
                caretWith = MathUtil.Clamp(value, 0.0f, float.MaxValue);
            }
        }

        /// <summary>
        /// Gets or sets the color of the selection.
        /// </summary>
        /// <userdoc>The color of the selection.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        public Color SelectionColor { get; set; } = Color.FromAbgr(0xF0F0F0FF);

        /// <summary>
        /// Gets or sets the color of the IME composition selection.
        /// </summary>
        /// <userdoc>The color of the selection.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        public Color IMESelectionColor { get; set; } = Color.FromAbgr(0xF0FFF0FF);

        /// <summary>
        /// Gets or sets whether the control is read-only, or not.
        /// </summary>
        /// <userdoc>True if the control is read-only, false otherwise.</userdoc>
        [DataMember]
        [Display(category: BehaviorCategory)]
        [DefaultValue(false)]
        public bool IsReadOnly
        {
            get { return isReadOnly; }
            set
            {
                if (IsReadOnly == value)
                    return;

                isReadOnly = value;
                OnIsReadOnlyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the edit text input type by setting a combination of <see cref="InputTypeFlags"/>.
        /// </summary>
        /// <userdoc>The edit text input type.</userdoc>
        [DataMember]
        [Display(category: BehaviorCategory)]
        [DefaultValue(default(InputTypeFlags))]
        public InputTypeFlags InputType
        {
            get { return inputType; }
            set
            {
                if (inputType == value)
                    return;

                inputType = value;

                UpdateTextToDisplay();

                UpdateInputTypeImpl();
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of characters that can be manually entered into the text box.
        /// </summary>
        /// <remarks>The value is coerced in the range [1, <see cref="int.MaxValue"/>].</remarks>
        /// <userdoc>The maximum number of characters that can be manually entered into the text box.</userdoc>
        [DataMember]
        [DataMemberRange(1, 0)]
        [Display(category: BehaviorCategory)]
        [DefaultValue(int.MaxValue)]
        public int MaxLength
        {
            get { return maxLength; }
            set
            {
                maxLength = MathUtil.Clamp(value, 1, int.MaxValue);
                OnMaxLengthChanged();
            }
        }

        /// <summary>
        /// Gets or sets the minimum number of visible lines.
        /// </summary>
        /// <remarks>The value is coerced in the range [1, <see cref="int.MaxValue"/>].</remarks>
        /// <userdoc>The minimum number of visible lines.</userdoc>
        [DataMember]
        [DataMemberRange(1, 0)]
        [Display(category: BehaviorCategory)]
        [DefaultValue(1)]
        public int MinLines
        {
            get { return minLines; }
            set
            {
                minLines = MathUtil.Clamp(value, 1, int.MaxValue);
                OnMinLinesChanged();
            }
        }

        /// <summary>
        /// Gets or sets the maximum number of visible lines.
        /// </summary>
        /// <remarks>The value is coerced in the range [int, <see cref="int.MaxValue"/>].</remarks>
        /// <userdoc>The maximum number of visible lines.</userdoc>
        [DataMember]
        [DataMemberRange(1, 0)]
        [Display(category: BehaviorCategory)]
        [DefaultValue(int.MaxValue)]
        public int MaxLines
        {
            get { return maxLines; }
            set
            {
                maxLines = MathUtil.Clamp(value, 1, int.MaxValue);
                OnMaxLinesChanged();
            }
        }

        /// <summary>
        /// Gets or sets the value indicating if the snapping of the <see cref="Text"/> of the <see cref="TextBlock"/> to the closest screen pixel should be skipped.
        /// </summary>
        /// <remarks>When <value>true</value>, the element's text is never snapped. 
        /// When <value>false</value>, it is snapped only if the font is dynamic and the element is rendered by a SceneUIRenderer.</remarks>
        /// <userdoc>True if the snapping of the Text to the closest screen pixel should be skipped, false otherwise.</userdoc>
        [DataMember]
        [Display(category: BehaviorCategory)]
        [DefaultValue(false)]
        public bool DoNotSnapText { get; set; } = false;

        [DataMemberIgnore]
        public int CompositionStart { get; private set; }

        [DataMemberIgnore]
        public int CompositionLength { get; private set; }

        [DataMemberIgnore]
        public string Composition { get; private set; } = "";

        /// <summary>
        /// Gets or sets the caret blinking frequency.
        /// </summary>
        /// <remarks>The value is coerced in the range [0, <see cref="float.MaxValue"/>].</remarks>
        /// <userdoc>The caret blinking frequency.</userdoc>
        [DataMember]
        [DataMemberRange(0, 3)]
        [Display(category: BehaviorCategory)]
        [DefaultValue(0)]
        public float CaretFrequency
        {
            get { return caretFrequency; }
            set
            {
                if (float.IsNaN(value))
                    return;
                caretFrequency = MathUtil.Clamp(value, 0.0f, float.MaxValue);
            }
        }

        /// <summary>
        /// Gets or sets the caret position in the <see cref="EditText"/>'s text.
        /// </summary>
        /// <userdoc>The caret position.</userdoc>
        [DataMemberIgnore]
        public int CaretPosition
        {
            get
            {
                UpdateSelectionFromEditImpl();

                return caretAtStart ? selectionStart : selectionStop;
            }
            set { Select(value, 0); }
        }

        /// <summary>
        /// Gets the value indicating if the blinking caret is currently visible or not.
        /// </summary>
        public bool IsCaretVisible => IsSelectionActive && !caretHidden;

        /// <summary>
        /// Reset the caret blinking to initial state (visible).
        /// </summary>
        public void ResetCaretBlinking()
        {
            caretHidden = false;
            accumulatedTime = 0f;
        }

        protected override void Update(GameTime time)
        {
            base.Update(time);

            if (!IsEnabled)
                return;

            if (IsSelectionActive)
            {
                accumulatedTime += (float)time.Elapsed.TotalSeconds;
                var displayTime = Math.Min(float.MaxValue, Math.Max(MathUtil.ZeroTolerance, 1 / (2 * CaretFrequency)));
                while (accumulatedTime > displayTime)
                {
                    accumulatedTime -= displayTime;
                    caretHidden = !caretHidden;
                }
            }
            else
            {
                ResetCaretBlinking();
            }
        }

        /// <summary>
        /// Gets the total number of lines in the text box.
        /// </summary>
        public int LineCount => GetLineCountImpl();

        /// <summary>
        /// Gets or sets the filter used to determine whether the inputed characters are valid or not.
        /// Accepted character are characters that the provided predicate function returns <value>true</value>.
        /// </summary>
        /// <remarks>If <see cref="CharacterFilterPredicate"/> is not set all characters are accepted.</remarks>
        [DataMemberIgnore]
        public Func<char, bool> CharacterFilterPredicate
        {
            get { return characterFilterPredicate; }
            set
            {
                if (characterFilterPredicate == value)
                    return;

                characterFilterPredicate = value;

                Text = text;
            }
        }

        /// <summary>
        /// Gets or sets the content of the current selection in the text box.
        /// </summary>
        /// <exception cref="ArgumentNullException">The provided string value is null</exception>
        [DataMemberIgnore]
        public string SelectedText
        {
            get { return Text.Substring(SelectionStart, SelectionLength); }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                var stringBfr = Text.Substring(0, SelectionStart);
                var stringAft = Text.Substring(SelectionStart + SelectionLength);

                Text = stringBfr + value + stringAft;
                CaretPosition = stringBfr.Length + value.Length;

                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating the number of characters in the current selection in the text box.
        /// </summary>
        /// <remarks>If the provided length of the selection is too big, the selection is extended until the end of the current text</remarks>
        [DataMemberIgnore]
        public int SelectionLength
        {
            get
            {
                UpdateSelectionFromEditImpl();

                return selectionStop - selectionStart;
            }
            set { Select(SelectionStart, value); }
        }

        /// <summary>
        /// Gets or sets a character index for the beginning of the current selection.
        /// </summary>
        /// <remarks>If the provided selection start index is too big, the caret is placed at the end of the current text</remarks>
        [DataMemberIgnore]
        public int SelectionStart
        {
            get
            {
                UpdateSelectionFromEditImpl();

                return selectionStart;
            }
            set { Select(value, SelectionLength); }
        }

        /// <summary>
        /// Gets or sets the text content of the text box.
        /// </summary>
        /// <remarks>Setting explicitly the text sets the cursor at the end of the new text.</remarks>
        /// <userdoc>The text content of the text box.</userdoc>
        [DataMember]
        [DefaultValue("")]
        public string Text
        {
            get { return text; }
            set
            {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));

                SetTextInternal(value, true);

                // remove all not valid characters 
                builder.Clear();
                var predicate = CharacterFilterPredicate;
                foreach (var character in value)
                {
                    if (predicate == null || predicate(character))
                        builder.Append(character);
                }

                SetTextInternal(builder.ToString(), true);
            }
        }

        /// <summary>
        /// Gets a value that indicates whether the is currently touched down.
        /// </summary>
        [DataMemberIgnore]
        protected virtual bool IsTouchedDown { get; set; }

        private void SetTextInternal(string newText, bool updateNativeEdit)
        {
            var truncatedText = newText;
            if (truncatedText.Length > MaxLength)
                truncatedText = truncatedText.Substring(0, MaxLength);

            var oldText = text;
            text = truncatedText;

            if (updateNativeEdit)
                UpdateTextToEditImpl();

            // early exit if text did not changed 
            if (text == oldText)
                return;

            UpdateTextToDisplay();

            Select(text.Length, 0);

            RaiseEvent(new RoutedEventArgs(TextChangedEvent));

            InvalidateMeasure();
        }

        private void UpdateTextToDisplay()
        {
            textToDisplay = ShouldHideText ? new string(PasswordHidingCharacter, text.Length) : text;
            if (Composition.Length > 0)
            {
                int insertPosition = CaretPosition;
                if (SelectionLength > 0)
                {
                    // Replace selection
                    textToDisplay = textToDisplay.Remove(selectionStart, SelectionLength);
                    if (!caretAtStart)
                        insertPosition -= SelectionLength;
                }
                
                // Insert composition into text to display
                if (insertPosition >= textToDisplay.Length)
                    textToDisplay += Composition;
                else
                    textToDisplay = textToDisplay.Insert(insertPosition, Composition);
            }
        }

        /// <summary>
        /// Returns the actual size of the text in virtual pixels unit.
        /// </summary>
        /// <remarks>If <see cref="TextSize"/> is <see cref="float.IsNaN"/>, returns the default size of the <see cref="Font"/>.</remarks>
        /// <seealso cref="TextSize"/>
        /// <seealso cref="SpriteFont.Size"/>
        public float ActualTextSize => !float.IsNaN(TextSize) ? TextSize : Font?.Size ?? 0;

        /// <summary>
        /// The actual text to show into the edit text.
        /// </summary>
        public string TextToDisplay => textToDisplay;

        /// <summary>
        /// Appends a string to the contents of a text control.
        /// </summary>
        /// <param name="textData">A string that specifies the text to append to the current contents of the text control.</param>
        public void AppendText(string textData)
        {
            if (textData == null)
                throw new ArgumentNullException(nameof(textData));

            Text += textData;
        }

        /// <summary>
        /// Selects all the contents of the text editing control.
        /// </summary>
        /// <param name="caretAtBeginning">Indicate if the caret should be at the beginning or the end of the selection</param>
        public void SelectAll(bool caretAtBeginning = false)
        {
            Select(0, Text.Length, caretAtBeginning);
        }

        /// <summary>
        /// Clears all the content from the text box.
        /// </summary>
        public void Clear()
        {
            Text = "";
        }

        /// <summary>
        /// Selects a range of text in the text box.
        /// </summary>
        /// <param name="start">The zero-based character index of the first character in the selection.</param>
        /// <param name="length">The length of the selection, in characters.</param>
        /// <param name="caretAtBeginning">Indicate if the caret should be at the beginning or the end of the selection</param>
        /// <remarks>If the value of start is too big the caret is positioned at the end of the current text.
        /// If the value of length is too big the selection is extended to the end current text.</remarks>
        public void Select(int start, int length, bool caretAtBeginning = false)
        {
            var truncatedStart = Math.Max(0, Math.Min(start, Text.Length));
            var truncatedStop = Math.Max(truncatedStart, Math.Min(Text.Length, truncatedStart + Math.Max(0, length)));

            selectionStart = truncatedStart;
            selectionStop = truncatedStop;
            caretAtStart = caretAtBeginning;

            ResetCaretBlinking(); // force caret not to blink when modifying selection/caret position.

            UpdateSelectionToEditImpl();
        }

        /// <summary>
        /// Calculate and returns the size of the <see cref="Text"/> in virtual pixels size.
        /// </summary>
        /// <returns>The size of the Text in virtual pixels.</returns>
        public Vector2 CalculateTextSize()
        {
            return CalculateTextSize(TextToDisplay);
        }

        /// <summary>
        /// Calculate and returns the size of the provided <paramref name="textToMeasure"/>"/> in virtual pixels size.
        /// </summary>
        /// <param name="textToMeasure">The text to measure</param>
        /// <returns>The size of the text in virtual pixels</returns>
        protected Vector2 CalculateTextSize(string textToMeasure)
        {
            if (textToMeasure == null || Font == null)
                return Vector2.Zero;

            var sizeRatio = LayoutingContext.RealVirtualResolutionRatio;
            var measureFontSize = new Vector2(ActualTextSize * sizeRatio.Y); // we don't want letters non-uniform ratio
            var realSize = Font.MeasureString(textToMeasure, measureFontSize);

            // force pre-generation if synchronous generation is required
            if (SynchronousCharacterGeneration)
                Font.PreGenerateGlyphs(textToMeasure, measureFontSize);

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

        protected override Vector3 MeasureOverride(Vector3 availableSizeWithoutMargins)
        {
            var desiredSize = Vector3.Zero;
            if (Font != null)
            {
                // take the maximum between the text size and the minimum visible line size as text desired size
                var fontLineSpacing = Font.GetTotalLineSpacing(ActualTextSize);
                if (Font.FontType == SpriteFontType.SDF)
                    fontLineSpacing *= ActualTextSize / Font.Size;
                var currentTextSize = new Vector3(CalculateTextSize(), 0);
                desiredSize = new Vector3(currentTextSize.X, Math.Min(Math.Max(currentTextSize.Y, fontLineSpacing * MinLines), fontLineSpacing * MaxLines), currentTextSize.Z);
            }

            // add the padding to the text desired size
            var desiredSizeWithPadding = CalculateSizeWithThickness(ref desiredSize, ref padding);

            return desiredSizeWithPadding;
        }

        protected override Vector3 ArrangeOverride(Vector3 finalSizeWithoutMargins)
        {
            // get the maximum between the final size and the desired size
            var returnSize = new Vector3(
                Math.Max(finalSizeWithoutMargins.X, DesiredSize.X),
                Math.Max(finalSizeWithoutMargins.Y, DesiredSize.Y),
                Math.Max(finalSizeWithoutMargins.Z, DesiredSize.Z));

            return returnSize;
        }

        /// <summary>
        /// Occurs when the text selection has changed.
        /// </summary>
        /// <remarks>A click event is bubbling</remarks>
        public event EventHandler<RoutedEventArgs> TextChanged
        {
            add { AddHandler(TextChangedEvent, value); }
            remove { RemoveHandler(TextChangedEvent, value); }
        }

        /// <summary>
        /// Identifies the <see cref="TextChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> TextChangedEvent = EventManager.RegisterRoutedEvent<RoutedEventArgs>(
            "TextChanged",
            RoutingStrategy.Bubble,
            typeof(EditText));

        private static void TextChangedClassHandler(object sender, RoutedEventArgs e)
        {
            var editText = (EditText)sender;

            editText.OnTextChanged(e);
        }

        /// <summary>
        /// The class handler of the event <see cref="TextChanged"/>.
        /// This method can be overridden in inherited classes to perform actions common to all instances of a class.
        /// </summary>
        /// <param name="args">The arguments of the event</param>
        protected virtual void OnTextChanged(RoutedEventArgs args)
        {
        }

        protected override void OnTouchDown(TouchEventArgs args)
        {
            base.OnTouchDown(args);

            IsSelectionActive = !IsReadOnly;
            IsTouchedDown = true;

            OnTouchDownImpl(args);
        }

        protected override void OnTouchUp(TouchEventArgs args)
        {
            base.OnTouchUp(args);

            if (IsTouchedDown)
            {
                OnTouchUpImpl(args);
            }
            IsTouchedDown = false;
        }

        protected override void OnTouchLeave(TouchEventArgs args)
        {
            base.OnTouchLeave(args);

            IsTouchedDown = false;
        }

        protected override void OnTouchMove(TouchEventArgs args)
        {
            base.OnTouchMove(args);

            if (IsTouchedDown)
            {
                OnTouchMoveImpl(args);
            }
        }
    }
}