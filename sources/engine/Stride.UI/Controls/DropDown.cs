// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Graphics;
using Stride.UI.Attributes;
using Stride.UI.Events;
using Stride.UI.Panels;

namespace Stride.UI.Controls
{
    /// <summary>
    /// Represents a dropdown control that presents a collapsible, scrollable list of selectable items.
    /// Clicking the header opens the list; selecting an item closes the list and updates the header to
    /// display the current selection.
    /// </summary>
    [DataContract(nameof(DropDown))]
    [DataContractMetadataType(typeof(DropDownMetadata))]
    [DebuggerDisplay("DropDown - Name={Name}")]
    [Display(category: InputCategory)]
    public class DropDown : Control
    {
        private int selectedIndex = -1;
        private bool isOpen;
        private float maxDropDownHeight = 300f;
        private Vector2 listOffset;
        private bool isPressed;
        private bool sizeToContent = true;
        private StretchType imageStretchType = StretchType.Uniform;
        private StretchDirection imageStretchDirection = StretchDirection.Both;
        private UIElement dismissTouchRoot;
        private EventHandler<TouchEventArgs> dismissTouchHandler;
        private readonly BackdropElement backdropElement;

        private ISpriteProvider openedImage;
        private ISpriteProvider closedImage;
        private ISpriteProvider mouseOverImage;
        private ISpriteProvider listBackground;
        private ISpriteProvider listItemNotPressedImage;
        private ISpriteProvider listItemPressedImage;
        private ISpriteProvider listItemMouseOverImage;
        private Color listItemBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 1f);
        private TextAlignment textAlignment = TextAlignment.Left;

        private readonly TrackingCollection<string> items = new TrackingCollection<string>();

        private Matrix headerContentWorldMatrix;

        private readonly TextBlock headerTextBlock;
        private readonly ScrollViewer popupScrollViewer;
        private readonly StackPanel itemStackPanel;

        private static readonly PropertyKey<Matrix> HeaderArrangeMatrixPropertyKey =
            DependencyPropertyFactory.RegisterAttached(nameof(HeaderArrangeMatrixPropertyKey), typeof(DropDown), Matrix.Identity);

        static DropDown()
        {
            EventManager.RegisterClassHandler(typeof(DropDown), SelectionChangedEvent, SelectionChangedClassHandler);
            EventManager.RegisterClassHandler(typeof(DropDown), DropDownOpenedEvent, DropDownOpenedClassHandler);
            EventManager.RegisterClassHandler(typeof(DropDown), DropDownClosedEvent, DropDownClosedClassHandler);
        }

        /// <summary>
        /// Creates a new instance of <see cref="DropDown"/>.
        /// </summary>
        public DropDown()
        {
            DrawLayerNumber += 2; // header image + list background image
            CanBeHitByUser = true;  // Warning: this must also match in DropDownMetadata

            itemStackPanel = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Name = "DropDownItemStackPanel",
            };

            popupScrollViewer = new ScrollViewer
            {
                ScrollMode = ScrollingMode.Vertical,
                ClipToBounds = true,
                Name = "DropDownPopupScrollViewer",
                Visibility = Visibility.Collapsed,
                Content = itemStackPanel,
                ScrollBarColor = ScrollBarColor,
                ScrollBarThickness = ScrollBarThickness,
            };

            headerTextBlock = new TextBlock
            {
                Name = "DropDownHeaderTextBlock",
                TextAlignment = TextAlignment.Left,
                VerticalAlignment = VerticalAlignment.Center,
            };

            SetVisualParent(headerTextBlock, this);

            backdropElement = new BackdropElement
            {
                Name = "DropDownBackdrop",
                CanBeHitByUser = true,
                Visibility = Visibility.Collapsed,
            };
            backdropElement.AddHandler(TouchDownEvent, (EventHandler<TouchEventArgs>)OnBackdropTouched);
            SetVisualParent(backdropElement, this);

            SetVisualParent(popupScrollViewer, this);

            MouseOverStateChanged += (sender, args) => InvalidateHeaderImage();
            items.CollectionChanged += OnItemsCollectionChanged;
        }

        // ---- Appearance ----

        /// <summary>
        /// Gets or sets the image displayed on the header when the dropdown list is open.
        /// </summary>
        /// <userdoc>Image displayed on the header when the dropdown list is open.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public ISpriteProvider OpenedImage
        {
            get => openedImage;
            set
            {
                if (openedImage == value)
                    return;

                openedImage = value;
                OnHeaderImageInvalidated();
            }
        }

        /// <summary>
        /// Gets or sets the image displayed on the header when the dropdown list is closed.
        /// </summary>
        /// <userdoc>Image displayed on the header when the dropdown list is closed.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public ISpriteProvider ClosedImage
        {
            get => closedImage;
            set
            {
                if (closedImage == value)
                    return;

                closedImage = value;
                OnHeaderImageInvalidated();
            }
        }

        /// <summary>
        /// Gets or sets the image displayed on the header when the mouse hovers over it.
        /// </summary>
        /// <userdoc>Image displayed on the header when the mouse hovers over it.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public ISpriteProvider MouseOverImage
        {
            get => mouseOverImage;
            set
            {
                if (mouseOverImage == value)
                    return;

                mouseOverImage = value;
                OnHeaderImageInvalidated();
            }
        }

        /// <summary>
        /// Gets or sets the color used to tint the header image. Default value is White.
        /// </summary>
        /// <remarks>The initial image color is multiplied by this color.</remarks>
        /// <userdoc>The color used to tint the header image. The default value is white.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        public Color HeaderColor { get; set; } = Color.White;

        /// <summary>
        /// Gets or sets a value that describes how the header image should be stretched to fill the destination rectangle.
        /// </summary>
        /// <remarks>This property has no effect if <see cref="SizeToContent"/> is <c>true</c>.</remarks>
        /// <userdoc>Describes how the header image should be stretched to fill the destination rectangle.</userdoc>
        [DataMember]
        [Display(category: LayoutCategory)]
        [DefaultValue(StretchType.Uniform)]
        public StretchType ImageStretchType
        {
            get => imageStretchType;
            set
            {
                imageStretchType = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets or sets a value that indicates how the header image is scaled.
        /// </summary>
        /// <remarks>This property has no effect if <see cref="SizeToContent"/> is <c>true</c>.</remarks>
        /// <userdoc>Indicates how the header image is scaled.</userdoc>
        [DataMember]
        [Display(category: LayoutCategory)]
        [DefaultValue(StretchDirection.Both)]
        public StretchDirection ImageStretchDirection
        {
            get => imageStretchDirection;
            set
            {
                imageStretchDirection = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets or sets whether the size of the control is driven by its text content. The default is <c>true</c>.
        /// </summary>
        /// <userdoc>True if the control sizes to its text content; false if it sizes to the header image.</userdoc>
        [DataMember]
        [Display(category: LayoutCategory)]
        [DefaultValue(true)]
        public bool SizeToContent
        {
            get => sizeToContent;
            set
            {
                if (sizeToContent == value)
                    return;

                sizeToContent = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets or sets the color of the header text. Default value is White.
        /// </summary>
        /// <userdoc>The color of the header text. The default value is white.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        public Color TextColor
        {
            get => headerTextBlock.TextColor;
            set => headerTextBlock.TextColor = value;
        }

        /// <summary>
        /// Gets or sets the font used to render the header text.
        /// </summary>
        /// <userdoc>The font used to render the header text.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public SpriteFont Font
        {
            get => headerTextBlock.Font;
            set => headerTextBlock.Font = value;
        }

        /// <summary>
        /// Gets or sets the size of the header text in virtual pixels.
        /// </summary>
        /// <userdoc>The size of the header text in virtual pixels.</userdoc>
        [DataMember]
        [DataMemberRange(0.0f, 3)]
        [Display(category: AppearanceCategory)]
        [DefaultValue(float.NaN)]
        public float TextSize
        {
            get => headerTextBlock.TextSize;
            set => headerTextBlock.TextSize = value;
        }

        /// <summary>
        /// Gets or sets the alignment of the header text. Default value is <see cref="Stride.Graphics.TextAlignment.Left"/>.
        /// </summary>
        /// <userdoc>The alignment of the header text.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(TextAlignment.Left)]
        public TextAlignment TextAlignment
        {
            get => textAlignment;
            set
            {
                if (textAlignment == value)
                    return;

                textAlignment = value;
                headerTextBlock.TextAlignment = value;

                if (isOpen)
                    RebuildItemButtons();
            }
        }

        /// <summary>
        /// Gets or sets the background image of the dropdown list popup.
        /// </summary>
        /// <userdoc>The background image of the dropdown list popup.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public ISpriteProvider ListBackground
        {
            get => listBackground;
            set
            {
                if (listBackground == value)
                    return;

                listBackground = value;
                OnListImageInvalidated();
            }
        }

        /// <summary>
        /// Gets or sets the color used to tint the list background image. Default value is White.
        /// </summary>
        /// <userdoc>The color used to tint the list background image. The default value is white.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        public Color ListColor { get; set; } = Color.White;

        /// <summary>
        /// Gets or sets the color of item text in the dropdown list. Default value is White.
        /// </summary>
        /// <userdoc>The color of item text in the dropdown list.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        public Color ItemTextColor { get; set; } = Color.White;

        /// <summary>
        /// Gets or sets the image displayed on list items in their default (not pressed) state.
        /// </summary>
        /// <userdoc>Image displayed on list items in their default state.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public ISpriteProvider ListItemNotPressedImage
        {
            get => listItemNotPressedImage;
            set
            {
                if (listItemNotPressedImage == value)
                    return;

                listItemNotPressedImage = value;
                OnListItemImageInvalidated();
            }
        }

        /// <summary>
        /// Gets or sets the image displayed on list items when they are pressed.
        /// </summary>
        /// <userdoc>Image displayed on list items when they are pressed.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public ISpriteProvider ListItemPressedImage
        {
            get => listItemPressedImage;
            set
            {
                if (listItemPressedImage == value)
                    return;

                listItemPressedImage = value;
                OnListItemImageInvalidated();
            }
        }

        /// <summary>
        /// Gets or sets the image displayed on list items when the mouse hovers over them.
        /// </summary>
        /// <userdoc>Image displayed on list items when the mouse hovers over them.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(null)]
        public ISpriteProvider ListItemMouseOverImage
        {
            get => listItemMouseOverImage;
            set
            {
                if (listItemMouseOverImage == value)
                    return;

                listItemMouseOverImage = value;
                OnListItemImageInvalidated();
            }
        }

        /// <summary>
        /// Gets or sets the color used to tint list item images. Default value is White.
        /// </summary>
        /// <userdoc>The color used to tint list item images. The default value is white.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        public Color ListItemColor { get; set; } = Color.White;

        /// <summary>
        /// Gets or sets the background color drawn behind each list item button.
        /// This is visible when no <see cref="ListItemNotPressedImage"/> is assigned and provides
        /// contrast so items are readable in the editor and in unstyled dropdowns.
        /// When a button image is set it renders on top and covers this color.
        /// </summary>
        /// <userdoc>Background color drawn behind each list item. Visible when no item image is assigned.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        public Color ListItemBackgroundColor
        {
            get => listItemBackgroundColor;
            set
            {
                listItemBackgroundColor = value;
                if (isOpen)
                    RebuildItemButtons();
            }
        }

        /// <summary>
        /// Gets or sets the color of the scrollbar inside the dropdown list.
        /// </summary>
        /// <userdoc>Color of the scrollbar inside the dropdown list.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        public Color ScrollBarColor
        {
            get => popupScrollViewer?.ScrollBarColor ?? new Color(0.1f, 0.1f, 0.1f, 1f);
            set
            {
                if (popupScrollViewer != null)
                    popupScrollViewer.ScrollBarColor = value;
            }
        }

        /// <summary>
        /// Gets or sets the thickness of the scrollbar in virtual pixels.
        /// </summary>
        /// <userdoc>The thickness of the scrollbar in virtual pixels.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(6.0f)]
        public float ScrollBarThickness
        {
            get => popupScrollViewer?.ScrollBarThickness ?? 6.0f;
            set
            {
                if (popupScrollViewer != null)
                    popupScrollViewer.ScrollBarThickness = value;
            }
        }

        // ---- Behavior ----

        /// <summary>
        /// Gets or sets the text shown in the header when no item is selected.
        /// </summary>
        /// <userdoc>The text shown in the header when no item is selected.</userdoc>
        [DataMember]
        [Display(category: BehaviorCategory)]
        [DefaultValue(null)]
        public string PlaceholderText
        {
            get => placeholderText;
            set
            {
                if (placeholderText == value)
                    return;

                placeholderText = value;
                UpdateHeaderText();
                InvalidateMeasure();
            }
        }
        private string placeholderText;

        /// <summary>
        /// Gets or sets the padding applied to each item button in the dropdown list.
        /// </summary>
        /// <userdoc>The padding applied to each item button in the dropdown list.</userdoc>
        [DataMember]
        [Display(category: BehaviorCategory)]
        public Thickness ItemPadding { get; set; }

        /// <summary>
        /// Gets or sets the maximum height of the dropdown list popup in virtual pixels.
        /// When the list content exceeds this height it becomes scrollable.
        /// </summary>
        /// <userdoc>The maximum height of the dropdown list popup in virtual pixels.</userdoc>
        [DataMember]
        [DataMemberRange(0.0f, 3)]
        [Display(category: BehaviorCategory)]
        [DefaultValue(300f)]
        public float MaxDropDownHeight
        {
            get => maxDropDownHeight;
            set
            {
                if (float.IsNaN(value))
                    return;

                maxDropDownHeight = MathUtil.Clamp(value, 0f, float.MaxValue);
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets or sets an offset applied to the dropdown list popup relative to the button.
        /// Positive X shifts the list to the right; positive Y shifts it down.
        /// </summary>
        /// <userdoc>An offset applied to the dropdown list popup relative to the button.</userdoc>
        [DataMember]
        [Display(category: BehaviorCategory)]
        public Vector2 ListOffset
        {
            get => listOffset;
            set
            {
                if (listOffset == value)
                    return;

                listOffset = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets the collection of strings displayed as items in the dropdown list.
        /// </summary>
        /// <userdoc>The items displayed in the dropdown list.</userdoc>
        [DataMember]
        [MemberCollection(CanReorderItems = true, NotNullItems = true)]
        [Display(category: BehaviorCategory)]
        public TrackingCollection<string> Items => items;

        /// <summary>
        /// Gets or sets the zero-based index of the currently selected item.
        /// A value of <c>-1</c> indicates no selection.
        /// </summary>
        /// <userdoc>The zero-based index of the currently selected item. -1 means no selection.</userdoc>
        [DataMember]
        [Display(category: BehaviorCategory)]
        [DefaultValue(-1)]
        public int SelectedIndex
        {
            get => selectedIndex;
            set
            {
                var clamped = (value >= -1 && value < items.Count) ? value : -1;
                if (selectedIndex == clamped)
                    return;

                selectedIndex = clamped;
                RaiseEvent(new RoutedEventArgs(SelectionChangedEvent));
            }
        }

        /// <summary>
        /// Gets or sets the currently selected item text, or <c>null</c> if no item is selected.
        /// Setting this property updates <see cref="SelectedIndex"/> to match the item's position in <see cref="Items"/>.
        /// </summary>
        [DataMemberIgnore]
        public string SelectedItem
        {
            get => (selectedIndex >= 0 && selectedIndex < items.Count) ? items[selectedIndex] : null;
            set => SelectedIndex = value != null ? items.IndexOf(value) : -1;
        }

        /// <summary>
        /// Gets or sets a value indicating whether the dropdown list is currently visible.
        /// </summary>
        /// <userdoc>True if the dropdown list is currently open, false otherwise.</userdoc>
        [DataMemberIgnore]
        public bool IsOpen
        {
            get => isOpen;
            set
            {
                if (isOpen == value)
                    return;

                isOpen = value;

                if (isOpen)
                {
                    RebuildItemButtons();
                    backdropElement.Visibility = Visibility.Visible;
                    popupScrollViewer.Visibility = Visibility.Visible;
                    SubscribeToDismissTouchHandler();
                }
                else
                {
                    UnsubscribeFromDismissTouchHandler();
                    popupScrollViewer.Visibility = Visibility.Collapsed;
                    backdropElement.Visibility = Visibility.Collapsed;
                }

                InvalidateMeasure();
                RaiseEvent(new RoutedEventArgs(isOpen ? DropDownOpenedEvent : DropDownClosedEvent));
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this dropdown responds to user input.
        /// Set to <c>false</c> by the editor to prevent interaction in the design viewport.
        /// </summary>
        [DataMemberIgnore]
        public bool IsInteractable { get; set; } = true;

        // ---- Internal renderer accessors ----

        /// <summary>
        /// Gets the appropriate header image provider based on the current open and mouse-over state.
        /// </summary>
        internal ISpriteProvider HeaderImageProvider =>
            MouseOverState == MouseOverState.MouseOverElement && MouseOverImage != null
                ? MouseOverImage
                : isOpen ? OpenedImage : ClosedImage;

        /// <summary>
        /// Gets the resolved header <see cref="Sprite"/> based on the current state.
        /// </summary>
        internal Sprite HeaderImage => HeaderImageProvider?.GetSprite();

        /// <summary>
        /// Gets the resolved list background <see cref="Sprite"/>.
        /// </summary>
        internal Sprite ListBackgroundSprite => ListBackground?.GetSprite();

        /// <summary>
        /// Gets the popup scroll viewer's world matrix for renderer use.
        /// </summary>
        internal ref Matrix PopupWorldMatrix => ref popupScrollViewer.WorldMatrixInternal;

        /// <summary>
        /// Gets the popup scroll viewer's render size for renderer use.
        /// </summary>
        internal ref Vector3 PopupRenderSize => ref popupScrollViewer.RenderSizeInternal;

        // ---- Events ----

        /// <summary>
        /// Occurs when <see cref="SelectedIndex"/> changes.
        /// </summary>
        /// <remarks>A SelectionChanged event is bubbling.</remarks>
        public event EventHandler<RoutedEventArgs> SelectionChanged
        {
            add { AddHandler(SelectionChangedEvent, value); }
            remove { RemoveHandler(SelectionChangedEvent, value); }
        }

        /// <summary>
        /// Identifies the <see cref="SelectionChanged"/> routed event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> SelectionChangedEvent =
            EventManager.RegisterRoutedEvent<RoutedEventArgs>("SelectionChanged", RoutingStrategy.Bubble, typeof(DropDown));

        /// <summary>
        /// Occurs when the dropdown list is opened.
        /// </summary>
        /// <remarks>A DropDownOpened event is bubbling.</remarks>
        public event EventHandler<RoutedEventArgs> DropDownOpened
        {
            add { AddHandler(DropDownOpenedEvent, value); }
            remove { RemoveHandler(DropDownOpenedEvent, value); }
        }

        /// <summary>
        /// Identifies the <see cref="DropDownOpened"/> routed event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> DropDownOpenedEvent =
            EventManager.RegisterRoutedEvent<RoutedEventArgs>("DropDownOpened", RoutingStrategy.Bubble, typeof(DropDown));

        /// <summary>
        /// Occurs when the dropdown list is closed.
        /// </summary>
        /// <remarks>A DropDownClosed event is bubbling.</remarks>
        public event EventHandler<RoutedEventArgs> DropDownClosed
        {
            add { AddHandler(DropDownClosedEvent, value); }
            remove { RemoveHandler(DropDownClosedEvent, value); }
        }

        /// <summary>
        /// Identifies the <see cref="DropDownClosed"/> routed event.
        /// </summary>
        public static readonly RoutedEvent<RoutedEventArgs> DropDownClosedEvent =
            EventManager.RegisterRoutedEvent<RoutedEventArgs>("DropDownClosed", RoutingStrategy.Bubble, typeof(DropDown));

        // ---- Layout ----

        /// <inheritdoc/>
        protected override Vector3 MeasureOverride(Vector3 availableSizeWithoutMargins)
        {
            Vector3 headerDesired;
            if (sizeToContent)
            {
                var childAvailable = CalculateSizeWithoutThickness(ref availableSizeWithoutMargins, ref padding);
                headerTextBlock.Measure(childAvailable);
                var headerDesiredSize = headerTextBlock.DesiredSizeWithMargins;

                var font = headerTextBlock.Font;
                if (font != null)
                {
                    var fontSize = float.IsNaN(headerTextBlock.TextSize) ? font.Size : headerTextBlock.TextSize;

                    if (placeholderText != null)
                        headerDesiredSize.X = Math.Max(headerDesiredSize.X, font.MeasureString(placeholderText, fontSize).X);

                    foreach (var item in items)
                        headerDesiredSize.X = Math.Max(headerDesiredSize.X, font.MeasureString(item, fontSize).X);
                }

                headerDesired = CalculateSizeWithThickness(ref headerDesiredSize, ref padding);
            }
            else
            {
                headerDesired = ImageSizeHelper.CalculateImageSizeFromAvailable(HeaderImage, availableSizeWithoutMargins, imageStretchType, imageStretchDirection, true);
            }

            if (isOpen)
            {
                var popupAvailable = new Vector3(availableSizeWithoutMargins.X, maxDropDownHeight, availableSizeWithoutMargins.Z);
                popupScrollViewer.Measure(popupAvailable);
            }

            return headerDesired;
        }

        /// <inheritdoc/>
        protected override Vector3 ArrangeOverride(Vector3 finalSizeWithoutMargins)
        {
            var arrangeSize = sizeToContent
                ? finalSizeWithoutMargins
                : ImageSizeHelper.CalculateImageSizeFromAvailable(HeaderImage, finalSizeWithoutMargins, imageStretchType, imageStretchDirection, false);

            var headerChildSize = CalculateSizeWithoutThickness(ref arrangeSize, ref padding);
            headerTextBlock.Arrange(headerChildSize, IsCollapsed);

            var headerOffsets = new Vector3(Padding.Left, Padding.Top, Padding.Front) - arrangeSize / 2;
            headerTextBlock.DependencyProperties.Set(HeaderArrangeMatrixPropertyKey, Matrix.Translation(headerOffsets));

            if (isOpen)
            {
                var popupHeight = MathUtil.Clamp(popupScrollViewer.DesiredSize.Y, 0, maxDropDownHeight);
                var popupSize = new Vector3(arrangeSize.X, popupHeight, arrangeSize.Z);
                popupScrollViewer.Arrange(popupSize, IsCollapsed);
            }

            return arrangeSize;
        }

        /// <inheritdoc/>
        protected override void UpdateWorldMatrix(ref Matrix parentWorldMatrix, bool parentWorldChanged)
        {
            var contentMatrixChanged = parentWorldChanged || ArrangeChanged || LocalMatrixChanged;

            base.UpdateWorldMatrix(ref parentWorldMatrix, parentWorldChanged);

            if (contentMatrixChanged)
            {
                var contentMatrix = headerTextBlock.DependencyProperties.Get(HeaderArrangeMatrixPropertyKey);
                Matrix.Multiply(ref contentMatrix, ref WorldMatrixInternal, out headerContentWorldMatrix);
            }
            ((IUIElementUpdate)headerTextBlock).UpdateWorldMatrix(ref headerContentWorldMatrix, contentMatrixChanged);

            if (isOpen && popupScrollViewer.IsArrangeValid)
            {
                var popupOffset = Matrix.Translation(-popupScrollViewer.RenderSize.X / 2f + listOffset.X, RenderSize.Y / 2f + listOffset.Y, 0f);
                Matrix.Multiply(ref popupOffset, ref WorldMatrixInternal, out var popupMatrix);
                ((IUIElementUpdate)popupScrollViewer).UpdateWorldMatrix(ref popupMatrix, true);
            }
        }

        // ---- Input handling ----

        /// <inheritdoc/>
        protected override void OnTouchDown(TouchEventArgs args)
        {
            if (!IsInteractable)
                return;
            base.OnTouchDown(args);
            if (ReferenceEquals(args.Source, this))
                isPressed = true;
        }

        /// <inheritdoc/>
        protected override void OnTouchUp(TouchEventArgs args)
        {
            if (!IsInteractable)
                return;
            base.OnTouchUp(args);

            if (isPressed)
                IsOpen = !isOpen;

            isPressed = false;
        }

        /// <inheritdoc/>
        protected override void OnTouchLeave(TouchEventArgs args)
        {
            if (!IsInteractable)
                return;
            base.OnTouchLeave(args);
            isPressed = false;
        }

        // ---- Item management ----

        private void OnItemsCollectionChanged(object sender, TrackingCollectionChangedEventArgs e)
        {
            if (selectedIndex >= items.Count)
                selectedIndex = items.Count > 0 ? items.Count - 1 : -1;

            UpdateHeaderText();

            if (isOpen)
                RebuildItemButtons();
        }

        private void RebuildItemButtons()
        {
            itemStackPanel.Children.Clear();

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                var index = i;

                UIElement itemContent = new TextBlock
                {
                    Text = item,
                    Font = Font,
                    TextSize = TextSize,
                    TextColor = ItemTextColor,
                    TextAlignment = textAlignment,
                    VerticalAlignment = VerticalAlignment.Center,
                };

                var itemButton = new Button
                {
                    Content = itemContent,
                    Padding = ItemPadding,
                    NotPressedImage = ListItemNotPressedImage,
                    PressedImage = ListItemPressedImage,
                    MouseOverImage = ListItemMouseOverImage,
                    Color = ListItemColor,
                    BackgroundColor = listItemBackgroundColor,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                };

                itemButton.Click += (s, a) => SelectItemAt(index);
                itemStackPanel.Children.Add(itemButton);
            }

            if (selectedIndex >= 0)
                itemStackPanel.ScrolllToElement(selectedIndex);
        }

        private void SelectItemAt(int index)
        {
            SelectedIndex = index;
            IsOpen = false;
        }

        private void UpdateHeaderText()
        {
            if (selectedIndex >= 0 && selectedIndex < items.Count)
                headerTextBlock.Text = items[selectedIndex];
            else
                headerTextBlock.Text = placeholderText;
        }

        private void SubscribeToDismissTouchHandler()
        {
            UIElement root = this;
            while (root.VisualParent != null)
                root = root.VisualParent;

            dismissTouchRoot = root;
            dismissTouchHandler = OnDismissTouchDown;
            dismissTouchRoot.AddHandler(TouchDownEvent, dismissTouchHandler, handledEventsToo: true);
        }

        private void UnsubscribeFromDismissTouchHandler()
        {
            if (dismissTouchRoot == null)
                return;

            dismissTouchRoot.RemoveHandler(TouchDownEvent, dismissTouchHandler);
            dismissTouchRoot = null;
            dismissTouchHandler = null;
        }

        private void OnDismissTouchDown(object sender, TouchEventArgs args)
        {
            var source = args.Source as UIElement;
            while (source != null)
            {
                if (ReferenceEquals(source, this))
                    return;
                source = source.VisualParent;
            }
            IsOpen = false;
        }

        private void OnBackdropTouched(object sender, TouchEventArgs args)
        {
            IsOpen = false;
        }

        // ---- Protected virtuals ----

        /// <summary>
        /// Called when one of the header images (<see cref="OpenedImage"/>, <see cref="ClosedImage"/>,
        /// or <see cref="MouseOverImage"/>) is invalidated.
        /// This method can be overridden in inherited classes.
        /// </summary>
        protected virtual void OnHeaderImageInvalidated()
        {
            InvalidateHeaderImage();
        }

        private void InvalidateHeaderImage()
        {
            if (!sizeToContent)
                InvalidateMeasure();
        }

        /// <summary>
        /// Called when the list background image (<see cref="ListBackground"/>) is invalidated.
        /// This method can be overridden in inherited classes.
        /// </summary>
        protected virtual void OnListImageInvalidated()
        {
        }

        /// <summary>
        /// Called when one of the list item images is invalidated.
        /// This method can be overridden in inherited classes.
        /// </summary>
        protected virtual void OnListItemImageInvalidated()
        {
            if (isOpen)
                RebuildItemButtons();
        }

        /// <summary>
        /// The class handler of the <see cref="SelectionChanged"/> event.
        /// This method can be overridden in inherited classes to perform actions common to all instances of a class.
        /// </summary>
        /// <param name="args">The arguments of the event.</param>
        protected virtual void OnSelectionChanged(RoutedEventArgs args)
        {
            UpdateHeaderText();
        }

        /// <summary>
        /// The class handler of the <see cref="DropDownOpened"/> event.
        /// This method can be overridden in inherited classes to perform actions common to all instances of a class.
        /// </summary>
        /// <param name="args">The arguments of the event.</param>
        protected virtual void OnDropDownOpened(RoutedEventArgs args)
        {
        }

        /// <summary>
        /// The class handler of the <see cref="DropDownClosed"/> event.
        /// This method can be overridden in inherited classes to perform actions common to all instances of a class.
        /// </summary>
        /// <param name="args">The arguments of the event.</param>
        protected virtual void OnDropDownClosed(RoutedEventArgs args)
        {
        }

        private static void SelectionChangedClassHandler(object sender, RoutedEventArgs args)
        {
            ((DropDown)sender).OnSelectionChanged(args);
        }

        private static void DropDownOpenedClassHandler(object sender, RoutedEventArgs args)
        {
            ((DropDown)sender).OnDropDownOpened(args);
        }

        private static void DropDownClosedClassHandler(object sender, RoutedEventArgs args)
        {
            ((DropDown)sender).OnDropDownClosed(args);
        }

        private sealed class BackdropElement : UIElement
        {
            protected internal override bool Intersects(ref Ray ray, out Vector3 intersectionPoint)
            {
                if (LayoutingContext == null)
                {
                    intersectionPoint = Vector3.Zero;
                    return false;
                }

                var resolution = LayoutingContext.VirtualResolution;
                var identity = Matrix.Identity;
                return CollisionHelper.RayIntersectsRectangle(ref ray, ref identity, ref resolution, 2, out intersectionPoint);
            }
        }

        private class DropDownMetadata
        {
            [DefaultThicknessValue(0, 0, 0, 0)]
            public Thickness Padding { get; }

            [DefaultValue(true)]
            public bool CanBeHitByUser { get; set; }
        }
    }
}
