// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.Games;

namespace Stride.UI
{
    /// <summary>
    /// Provides a base class for all the User Interface elements in Stride applications.
    /// </summary>
    [DataContract(Inherited = true)]
    [CategoryOrder(10, AppearanceCategory, Expand = ExpandRule.Auto)]
    [CategoryOrder(20, BehaviorCategory, Expand = ExpandRule.Auto)]
    [CategoryOrder(30, LayoutCategory, Expand = ExpandRule.Auto)]
    [CategoryOrder(100, MiscCategory, Expand = ExpandRule.Auto)]
    [DebuggerDisplay("UIElement: {Name}")]
    public abstract partial class UIElement : IUIElementUpdate, IUIElementChildren, IIdentifiable
    {
        // Categories of UI element classes
        protected const string InputCategory = "Input";
        protected const string PanelCategory = "Panel";
        // Categories of UI element properties
        protected const string AppearanceCategory = "Appearance";
        protected const string BehaviorCategory = "Behavior";
        protected const string LayoutCategory = "Layout";
        protected const string MiscCategory = "Misc";

        internal Size2F RenderSizeInternal;
        internal Matrix WorldMatrixInternal;
        internal Matrix WorldMatrixPickingInternal;
        protected internal Thickness MarginInternal = Thickness.Uniform(0f);

        private string name;
        private Visibility visibility = Visibility.Visible;
        private float opacity = 1.0f;
        private bool isEnabled = true;
        private bool isHierarchyEnabled = true;
        private float defaultWidth;
        private float defaultHeight;
        private float width = float.NaN;
        private float height = float.NaN;
        private float depthOffset = 0;
        private HorizontalAlignment horizontalAlignment = HorizontalAlignment.Stretch;
        private VerticalAlignment verticalAlignment = VerticalAlignment.Stretch;
        private float maximumWidth = float.PositiveInfinity;
        private float maximumHeight = float.PositiveInfinity;
        private float minimumWidth;
        private float minimumHeight;
        private Matrix localMatrix = Matrix.Identity;
        private MouseOverState mouseOverState;
        private LayoutingContext layoutingContext;

        protected bool ArrangeChanged;
        protected bool LocalMatrixChanged;

        private Size2F previousProvidedMeasureSize = new Size2F(-1,-1);
        private Size2F previousProvidedArrangeSize = new Size2F(-1,-1);
        private bool previousIsParentCollapsed;

        /// <summary>
        /// Creates a new instance of <see cref="UIElement"/>.
        /// </summary>
        protected UIElement()
        {
            Id = Guid.NewGuid();
            DependencyProperties = new PropertyContainerClass(this);
            VisualChildrenCollection = new UIElementCollection();
        }

        /// <summary>
        /// The <see cref="UIElement"/> that currently has the focus.
        /// </summary>
        internal static UIElement FocusedElement { get; set; }

        /// <summary>
        /// A unique ID defining the UI element.
        /// </summary>
        /// <userdoc>A unique ID defining the UI element.</userdoc>
        [DataMember]
        [Display(Browsable = false)]
        [NonOverridable]
        public Guid Id { get; set; }

        /// <summary>
        /// The list of the dependency properties attached to the UI element.
        /// </summary>
        /// <userdoc>The list of the dependency properties attached to the UI element.</userdoc>
        [DataMember]
#pragma warning disable STRDIAG009 // Invalid Dictionary Key - PropertyKey has the TypeConverter PropertyKeyNameResolver so it's valid.
        public PropertyContainerClass DependencyProperties { get; }
#pragma warning restore STRDIAG009 // Invalid Dictionary Key

        /// <summary>
        /// Gets or sets the LocalMatrix of this element.
        /// </summary>
        /// <remarks>The local transform is not taken is account during the layering. The transformation is purely for rendering effects.</remarks>
        /// <userdoc>Local matrix of this element.</userdoc>
        [DataMemberIgnore]
        public Matrix LocalMatrix
        {
            get => localMatrix;
            set
            {
                localMatrix = value;
                LocalMatrixChanged = true;
            }
        }

        /// <summary>
        /// The background color of the element.
        /// </summary>
        /// <userdoc>Color used for the background surface of this element.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        public Color BackgroundColor { get; set; }

        /// <summary>
        /// Gets or sets the opacity factor applied to the entire UIElement when it is rendered in the user interface (UI).
        /// </summary>
        /// <remarks>The value is coerced in the range [0, 1].</remarks>
        /// <userdoc>Opacity factor applied to this element when rendered in the user interface (UI).</userdoc>
        [DataMember]
        [DataMemberRange(0.0f, 1.0f, 0.01f, 0.1f, 2)]
        [Display(category: AppearanceCategory)]
        [DefaultValue(1.0f)]
        public float Opacity
        {
            get => opacity;
            set
            {
                if (float.IsNaN(value))
                    return;
                opacity = MathUtil.Clamp(value, 0.0f, 1.0f);
            }
        }

        /// <summary>
        /// Gets or sets the user interface (UI) visibility of this element.
        /// </summary>
        /// <userdoc>Visibility of this element.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(Visibility.Visible)]
        public Visibility Visibility
        {
            get => visibility;
            set
            {
                if (value == visibility)
                    return;

                visibility = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether to clip the content of this element (or content coming from the child elements of this element)
        /// to fit into the size of the containing element.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">The value has to be positive and finite.</exception>
        /// <userdoc>Indicates whether to clip the content of this element (or content coming from the child elements of this element).</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(false)]
        public bool ClipToBounds { get; set; } = false;

        /// <summary>
        /// The number of layers used to draw this element.
        /// This value has to be modified by the user when he redefines the default element renderer,
        /// so that <see cref="DepthBias"/> values of the relatives keeps enough spaces to draw the different layers.
        /// </summary>
        /// <userdoc>The number of layers used to draw this element.</userdoc>
        [DataMember]
        [Display(category: AppearanceCategory)]
        [DefaultValue(1)]
        public int DrawLayerNumber { get; set; } = 1; // one layer for BackgroundColor/Clipping

        /// <summary>
        /// Gets or sets a value indicating whether this element is enabled in the user interface (UI).
        /// </summary>
        /// <userdoc>True if this element is enabled, False otherwise.</userdoc>
        [DataMember]
        [Display(category: BehaviorCategory)]
        [DefaultValue(true)]
        public virtual bool IsEnabled
        {
            get => isEnabled;
            set
            {
                isEnabled = value;

                MouseOverState = MouseOverState.MouseOverNone;
            }
        }

        /// <summary>
        /// Indicate if the UIElement can be hit by the user.
        /// If this property is true, the UI system performs hit test on the UIElement.
        /// </summary>
        /// <userdoc>True if the UI system should perform hit test on this element, False otherwise.</userdoc>
        [DataMember]
        [Display(category: BehaviorCategory)]
        [DefaultValue(false)]
        public bool CanBeHitByUser { get; set; }

        /// <summary>
        /// Gets or sets the user suggested width of this element.
        /// </summary>
        /// <remarks>The value is coerced in the range [0, <see cref="float.MaxValue"/>].</remarks>
        /// <userdoc>Width of this element. If NaN, the default width will be used instead.</userdoc>
        [DataMember]
        [DataMemberRange(0.0f, 3)]
        [Display(category: LayoutCategory)]
        [DefaultValue(float.NaN)]
        public float Width
        {
            get => width;
            set
            {
                width = MathUtil.Clamp(value, 0.0f, float.MaxValue);
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets or sets the user suggested height of this element.
        /// </summary>
        /// <remarks>The value is coerced in the range [0, <see cref="float.MaxValue"/>].</remarks>
        /// <userdoc>Height of this element. If NaN, the default height will be used instead.</userdoc>
        [DataMember]
        [DataMemberRange(0.0f, 3)]
        [Display(category: LayoutCategory)]
        [DefaultValue(float.NaN)]
        public float Height
        {
            get => height;
            set
            {
                height = MathUtil.Clamp(value, 0.0f, float.MaxValue);
                InvalidateMeasure();
            }
        }

        [DataMember]
        [Display(category: LayoutCategory)]
        [DefaultValue(0)]
        public float DepthOffset
        {
            get => depthOffset;
            set
            {
                depthOffset = value;
                InvalidateArrange();
            }
        }

        /// <summary>
        /// Gets or sets the size of the element. Same as setting separately <see cref="Width"/>, and <see cref="Height"/>.
        /// </summary>
        [DataMemberIgnore]
        public Size2F Size
        {
            get => new Size2F(Width, Height);
            set
            {
                Width = value.Width;
                Height = value.Height;
            }
        }

        /// <summary>
        /// Gets or sets the horizontal alignment of this element.
        /// </summary>
        /// <userdoc>Horizontal alignment of this element.</userdoc>
        [DataMember]
        [Display(category: LayoutCategory)]
        [DefaultValue(HorizontalAlignment.Stretch)]
        public HorizontalAlignment HorizontalAlignment
        {
            get => horizontalAlignment;
            set
            {
                horizontalAlignment = value;
                InvalidateArrange();
            }
        }

        /// <summary>
        /// Gets or sets the vertical alignment of this element.
        /// </summary>
        /// <userdoc>Vertical alignment of this element.</userdoc>
        [DataMember]
        [Display(category: LayoutCategory)]
        [DefaultValue(VerticalAlignment.Stretch)]
        public VerticalAlignment VerticalAlignment
        {
            get => verticalAlignment;
            set
            {
                verticalAlignment = value;
                InvalidateArrange();
            }
        }

        /// <summary>
        /// Gets or sets the margins of this element.
        /// </summary>
        /// <userdoc>Layout margin of this element.</userdoc>
        [DataMember]
        [Display(category: LayoutCategory)]
        public Thickness Margin
        {
            get => MarginInternal;
            set
            {
                MarginInternal = value;
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets or sets the minimum width of this element.
        /// </summary>
        /// <remarks>The value is coerced in the range [0, <see cref="float.MaxValue"/>].</remarks>
        /// <userdoc>Minimum width of this element.</userdoc>
        [DataMember]
        [DataMemberRange(0.0f, 3)]
        [Display(category: LayoutCategory)]
        [DefaultValue(0.0f)]
        public float MinimumWidth
        {
            get => minimumWidth;
            set
            {
                if (float.IsNaN(value))
                    return;
                minimumWidth = MathUtil.Clamp(value, 0.0f, float.MaxValue);
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets or sets the minimum height of this element.
        /// </summary>
        /// <remarks>The value is coerced in the range [0, <see cref="float.MaxValue"/>].</remarks>
        /// <userdoc>Minimum height of this element.</userdoc>
        [DataMember]
        [DataMemberRange(0.0f, 3)]
        [Display(category: LayoutCategory)]
        [DefaultValue(0.0f)]
        public float MinimumHeight
        {
            get => minimumHeight;
            set
            {
                if (float.IsNaN(value))
                    return;
                minimumHeight = MathUtil.Clamp(value, 0.0f, float.MaxValue);
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets or sets the maximum width of this element.
        /// </summary>
        /// <remarks>The value is coerced in the range [0, <see cref="float.PositiveInfinity"/>].</remarks>
        /// <userdoc>Maximum width of this element.</userdoc>
        [DataMember]
        [DataMemberRange(0.0f, 3)]
        [Display(category: LayoutCategory)]
        [DefaultValue(float.PositiveInfinity)]
        public float MaximumWidth
        {
            get => maximumWidth;
            set
            {
                if (float.IsNaN(value))
                    return;
                maximumWidth = MathUtil.Clamp(value, 0.0f, float.PositiveInfinity);
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets or sets the maximum height of this element.
        /// </summary>
        /// <remarks>The value is coerced in the range [0, <see cref="float.PositiveInfinity"/>].</remarks>
        /// <userdoc>Maximum height of this element.</userdoc>
        [DataMember]
        [DataMemberRange(0.0f, 3)]
        [Display(category: LayoutCategory)]
        [DefaultValue(float.PositiveInfinity)]
        public float MaximumHeight
        {
            get => maximumHeight;
            set
            {
                if (float.IsNaN(value))
                    return;
                maximumHeight = MathUtil.Clamp(value, 0.0f, float.PositiveInfinity);
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets or sets the default width of this element.
        /// </summary>
        /// <remarks>The value is coerced in the range [0, <see cref="float.MaxValue"/>].</remarks>
        /// <userdoc>Default width of this element.</userdoc>
        [DataMember]
        [DataMemberRange(0.0f, 3)]
        [Display(category: LayoutCategory)]
        [DefaultValue(0.0f)]
        public float DefaultWidth
        {
            get => defaultWidth;
            set
            {
                if (float.IsNaN(value))
                    return;
                defaultWidth = MathUtil.Clamp(value, 0.0f, float.MaxValue);
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets or sets the default height of this element.
        /// </summary>
        /// <remarks>The value is coerced in the range [0, <see cref="float.MaxValue"/>].</remarks>
        /// <userdoc>Default height of this element.</userdoc>
        [DataMember]
        [DataMemberRange(0.0f, 3)]
        [Display(category: LayoutCategory)]
        [DefaultValue(0.0f)]
        public float DefaultHeight
        {
            get => defaultHeight;
            set
            {
                if (float.IsNaN(value))
                    return;
                defaultHeight = MathUtil.Clamp(value, 0.0f, float.MaxValue);
                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets or sets the name of this element.
        /// </summary>
        /// <userdoc>Name of this element.</userdoc>
        [DataMember]
        [Display(category: MiscCategory)]
        [DefaultValue(null)]
        public string Name
        {
            get => name;
            set
            {
                if (name == value)
                    return;

                name = value;
                OnNameChanged();
            }
        }

        /// <summary>
        /// Gets the size that this element computed during the measure pass of the layout process.
        /// </summary>
        /// <remarks>This value does not contain possible <see cref="Margin"/></remarks>
        [DataMemberIgnore]
        public Size2F DesiredSize { get; private set; }

        /// <summary>
        /// Gets the size that this element computed during the measure pass of the layout process.
        /// </summary>
        /// <remarks>This value contains possible <see cref="Margin"/></remarks>
        [DataMemberIgnore]
        public Size2F DesiredSizeWithMargins { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the computed size and position of child elements in this element's layout are valid.
        /// </summary>
        [DataMemberIgnore]
        public bool IsArrangeValid { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the current size returned by layout measure is valid.
        /// </summary>
        [DataMemberIgnore]
        public bool IsMeasureValid { get; private set; }

        /// <summary>
        /// The world matrix of the UIElement.
        /// The origin of the element is the center of the object's bounding box defined by <see cref="RenderSize"/>.
        /// </summary>
        [DataMemberIgnore]
        public Matrix WorldMatrix
        {
            get => WorldMatrixInternal;
            private set => WorldMatrixInternal = value;
        }

        /// <summary>
        /// The final depth bias value of the element resulting from the parent/children z order update.
        /// </summary>
        [DataMemberIgnore]
        public int DepthBias { get; private set; }

        /// <summary>
        /// The maximum depth bias value among the children of the element resulting from the parent/children z order update.
        /// </summary>
        internal int MaxChildrenDepthBias { get; private set; }

        internal bool ForceNextMeasure = true;
        internal bool ForceNextArrange = true;

        /// <summary>
        /// The ratio between the element real size on the screen and the element virtual size.
        /// </summary>
        protected internal LayoutingContext LayoutingContext
        {
            get => layoutingContext;
            set
            {
                if (value == null)
                    return;

                if (layoutingContext != null && layoutingContext.Equals(value))
                    return;

                ForceMeasure();
                layoutingContext = value;
                foreach (var child in VisualChildrenCollection)
                    child.LayoutingContext = value;
            }
        }

        private UIElementServices uiElementServices;

        internal UIElementServices UIElementServices
        {
            get
            {
                if (Parent != null && !Parent.UIElementServices.Equals(ref uiElementServices))
                    uiElementServices = Parent.UIElementServices;

                return uiElementServices;
            }
            set
            {
                if (Parent != null)
                    throw new InvalidOperationException("Can only assign UIElementService to the root element!");

                uiElementServices = value;
            }
        }

        /// <summary>
        /// The visual children of this element.
        /// </summary>
        /// <remarks>If the class is inherited it is the responsibility of the descendant class to correctly update this collection</remarks>
        [DataMemberIgnore]
        protected internal UIElementCollection VisualChildrenCollection { get; }

        /// <summary>
        /// Invalidates the arrange state (layout) for the element.
        /// </summary>
        protected internal void InvalidateArrange()
        {
            ForceArrange(); // force arrange on top hierarchy

            PropagateArrangeInvalidationToChildren(); // propagate weak invalidation on children
        }

        private void PropagateArrangeInvalidationToChildren()
        {
            foreach (var child in VisualChildrenCollection)
            {
                if (!child.IsArrangeValid)
                    continue;

                child.IsArrangeValid = false;
                child.PropagateArrangeInvalidationToChildren();
            }
        }

        private void ForceArrange()
        {
            if (ForceNextArrange) // no need to propagate arrange force if it's already done
                return;

            IsArrangeValid = false;
            ForceNextArrange = true;

            VisualParent?.ForceArrange();
        }

        /// <summary>
        /// Invalidates the measurement state (layout) for the element.
        /// </summary>
        protected internal void InvalidateMeasure()
        {
            ForceMeasure(); // force measure on top hierarchy

            PropagateMeasureInvalidationToChildren(); // propagate weak invalidation on children
        }

        private void PropagateMeasureInvalidationToChildren()
        {
            foreach (var child in VisualChildrenCollection)
            {
                if (child.IsMeasureValid)
                {
                    child.IsMeasureValid = false;
                    child.IsArrangeValid = false;
                    child.PropagateMeasureInvalidationToChildren();
                }
            }
        }

        private void ForceMeasure()
        {
            if (ForceNextMeasure && ForceNextArrange) // no need to propagate arrange force if it's already done
                return;

            ForceNextMeasure = true;
            ForceNextArrange = true;

            IsMeasureValid = false;
            IsArrangeValid = false;

            VisualParent?.ForceMeasure();
        }

        /// <summary>
        /// This method is call when the name of the UIElement changes.
        /// This method can be overridden in inherited classes to perform class specific actions on <see cref="Name"/> changes.
        /// </summary>
        protected virtual void OnNameChanged()
        {
        }

        /// <summary>
        /// Gets the value indicating whether this element and all its upper hierarchy are enabled or not.
        /// </summary>
        public bool IsHierarchyEnabled => isHierarchyEnabled;

        /// <summary>
        /// Gets a value indicating whether this element is visible in the user interface (UI).
        /// </summary>
        public bool IsVisible => Visibility == Visibility.Visible;

        /// <summary>
        /// Gets a value indicating whether this element takes some place in the user interface.
        /// </summary>
        public bool IsCollapsed => Visibility == Visibility.Collapsed;

        /// <summary>
        /// Set one component of the size of the element.
        /// </summary>
        /// <param name="dimensionIndex">Index indicating which component to set</param>
        /// <param name="value">The value to give to the size</param>
        internal void SetSize(int dimensionIndex, float value)
        {
            if (dimensionIndex == 0)
                Width = value;
            else
                Height = value;
        }

        /// <summary>
        /// Gets the logical parent of this element.
        /// </summary>
        [DataMemberIgnore]
        [CanBeNull]
        public UIElement Parent { get; protected set; }

        /// <summary>
        /// Gets the visual parent of this element.
        /// </summary>
        [DataMemberIgnore]
        [CanBeNull]
        public UIElement VisualParent { get; protected set; }


        /// <summary>
        /// Get a enumerable to the visual children of the <see cref="UIElement"/>.
        /// </summary>
        /// <remarks>Inherited classes are in charge of overriding this method to return their children.</remarks>
        [DataMemberIgnore]
        public IReadOnlyList<UIElement> VisualChildren => VisualChildrenCollection;

        /// <summary>
        /// The list of the children of the element that can be hit by the user.
        /// </summary>
        protected internal virtual FastCollection<UIElement> HitableChildren => VisualChildrenCollection;

        /// <summary>
        /// The opacity used to render element.
        /// </summary>
        [DataMemberIgnore]
        public float RenderOpacity { get; private set; }

        /// <summary>
        /// Gets (or sets, but see Remarks) the final render size of this element.
        /// </summary>
        [DataMemberIgnore]
        public Size2F RenderSize
        {
            get => RenderSizeInternal;
            private set => RenderSizeInternal = value;
        }

        /// <summary>
        /// The rendering offsets caused by the UIElement margins and alignments.
        /// </summary>
        [DataMemberIgnore]
        public Vector3 RenderOffsets { get; private set; }

        /// <summary>
        /// Gets the rendered width of this element.
        /// </summary>
        public float ActualWidth => RenderSize.Width;

        /// <summary>
        /// Gets the rendered height of this element.
        /// </summary>
        public float ActualHeight => RenderSize.Height;

        /// <inheritdoc/>
        IEnumerable<IUIElementChildren> IUIElementChildren.Children => EnumerateChildren();

        /// <summary>
        /// Enumerates the children of this element.
        /// </summary>
        /// <returns>A sequence containing all the children of this element.</returns>
        /// <remarks>This method is used by the implementation of the <see cref="IUIElementChildren"/> interface.</remarks>
        protected virtual IEnumerable<IUIElementChildren> EnumerateChildren()
        {
            // Empty by default
            yield break;
        }

        private unsafe bool Size2BinaryEqual(ref Size2F left, ref Size2F right)
        {
            fixed (Size2F* pVector2Left = &left)
            fixed (Size2F* pVector2Right = &right)
            {
                var pLeft = (int*)pVector2Left;
                var pRight = (int*)pVector2Right;

                return pLeft[0] == pRight[0] && pLeft[1] == pRight[1];
            }
        }

        /// <summary>
        /// Updates the <see cref="DesiredSize"/> of a <see cref="UIElement"/>.
        /// Parent elements call this method from their own implementations to form a recursive layout update.
        /// Calling this method constitutes the first pass (the "Measure" pass) of a layout update.
        /// </summary>
        /// <param name="availableSizeWithMargins">The available space that a parent element can allocate a child element with its margins.
        /// A child element can request a larger space than what is available;  the provided size might be accommodated if scrolling is possible in the content model for the current element.</param>
        public void Measure(Size2F availableSizeWithMargins)
        {
            if (!ForceNextMeasure && Size2BinaryEqual(ref availableSizeWithMargins, ref previousProvidedMeasureSize))
            {
                IsMeasureValid = true;
                ValidateChildrenMeasure();
                return;
            }

            ForceNextMeasure = false;
            IsMeasureValid = true;
            IsArrangeValid = false;
            RequiresMouseOverUpdate = true;
            previousProvidedMeasureSize = availableSizeWithMargins;

            // avoid useless computation if the element is collapsed
            if (IsCollapsed)
            {
                DesiredSize = DesiredSizeWithMargins = Size2F.Zero;
                return;
            }

            // variable containing the temporary desired size
            var desiredSize = new Size2F(Width, Height);

            // width, or height of the UIElement might be undetermined
            // -> compute the desired size of the children to determine it

            // removes the size required for the margins in the available size
            var availableSizeWithoutMargins = availableSizeWithMargins - MarginInternal;

            // clamp the available size for the element between the maximum and minimum width/height of the UIElement
            availableSizeWithoutMargins = new Size2F(
                Math.Max(MinimumWidth, Math.Min(MaximumWidth, !float.IsNaN(desiredSize.Width) ? desiredSize.Width : availableSizeWithoutMargins.Width)),
                Math.Max(MinimumHeight, Math.Min(MaximumHeight, !float.IsNaN(desiredSize.Height) ? desiredSize.Height : availableSizeWithoutMargins.Height)));

            // compute the desired size for the children
            var childrenDesiredSize = MeasureOverride(availableSizeWithoutMargins);

            // replace the undetermined size by the desired size for the children
            if (float.IsNaN(desiredSize.Width))
                desiredSize.Width = childrenDesiredSize.Width;
            if (float.IsNaN(desiredSize.Height))
                desiredSize.Height = childrenDesiredSize.Height;

            // override the element size by the default size if still unspecified
            if (float.IsNaN(desiredSize.Width))
                desiredSize.Width = DefaultWidth;
            if (float.IsNaN(desiredSize.Height))
                desiredSize.Height = DefaultHeight;

            // replace infinity with the parent's RenderSize
            if (float.IsInfinity(desiredSize.X) || float.IsInfinity(desiredSize.Y) || float.IsInfinity(desiredSize.Z))
                desiredSize = InfinityToParentRenderSize(desiredSize);

            // clamp the desired size between the maximum and minimum width/height of the UIElement
            desiredSize = new Size2F(
                Math.Max(MinimumWidth, Math.Min(MaximumWidth, desiredSize.Width)),
                Math.Max(MinimumHeight, Math.Min(MaximumHeight, desiredSize.Height)));

            // compute the desired size with margin
            var desiredSizeWithMargins = desiredSize + MarginInternal;

            // update Element state variables
            DesiredSize = desiredSize;
            DesiredSizeWithMargins = desiredSizeWithMargins;
        }

        /// <summary>
        /// Recursively checks parents for infinity values, replacing infinity with the Parent's <see cref="RenderSize"/>.
        /// </summary>
        /// <param name="size">The size vector with possible infinity values to replace.</param>
        /// <returns>The input <paramref name="size"/> value with the infinities replaced with the Parent's <see cref="RenderSize"/>.</returns>
        internal Vector3 InfinityToParentRenderSize(Vector3 size)
        {
            for (int i = 0; i < 3; i++)
            {
                if (float.IsInfinity(size[i]))
                    size[i] = Parent.RenderSize[i];
            }
            if (float.IsInfinity(size.X) || float.IsInfinity(size.Y) || float.IsInfinity(size.Z))
                return Parent.InfinityToParentRenderSize(size);

            return size;
        }

        private void ValidateChildrenMeasure()
        {
            foreach (var child in VisualChildrenCollection)
            {
                if (!child.IsMeasureValid)
                {
                    child.IsMeasureValid = true;
                    child.ValidateChildrenMeasure();
                }
            }
        }

        /// <summary>
        /// When overridden in a derived class, measures the size in layout required for possible child elements and determines a size for the <see cref="UIElement"/>-derived class.
        /// </summary>
        /// <param name="availableSizeWithoutMargins">The available size that this element can give to child elements.
        /// Infinity can be specified as a value to indicate that the element will size to whatever content is available.</param>
        /// <returns>The size desired by the children</returns>
        protected virtual Size2F MeasureOverride(Size2F availableSizeWithoutMargins)
        {
            return Size2F.Zero;
        }

        /// <summary>
        /// Positions child elements and determines the size of the UIElement.
        /// This method constitutes the second pass of a layout update.
        /// </summary>
        /// <param name="finalSizeWithMargins">The final size that the parent computes for the child element with the margins.</param>
        /// <param name="isParentCollapsed">Boolean indicating if one of the parents of the element is currently collapsed.</param>
        public void Arrange(Size2F finalSizeWithMargins, bool isParentCollapsed)
        {
            if (!ForceNextArrange && Size2BinaryEqual(ref finalSizeWithMargins, ref previousProvidedArrangeSize) && isParentCollapsed == previousIsParentCollapsed)
            {
                IsArrangeValid = true;
                ValidateChildrenArrange();
                return;
            }

            ForceNextArrange = false;
            IsArrangeValid = true;
            ArrangeChanged = true;
            previousIsParentCollapsed = isParentCollapsed;
            previousProvidedArrangeSize = finalSizeWithMargins;
            RequiresMouseOverUpdate = true;

            // special to avoid useless computation if the element is collapsed
            if (IsCollapsed || isParentCollapsed)
            {
                CollapseOverride();
                return;
            }

            // initialize the element size with the user suggested size (maybe NaN if not set)
            var elementSize = new Size2F(Width, Height);

            // stretch the element if the user size is unspecified and alignment constraints requires it
            var finalSizeWithoutMargins = finalSizeWithMargins - MarginInternal;
            if (float.IsNaN(elementSize.Width) && HorizontalAlignment == HorizontalAlignment.Stretch)
                elementSize.Width = finalSizeWithoutMargins.Width;
            if (float.IsNaN(elementSize.Height) && VerticalAlignment == VerticalAlignment.Stretch)
                elementSize.Height = finalSizeWithoutMargins.Height;

            // override the element size by the desired size if still unspecified
            if (float.IsNaN(elementSize.Width))
                elementSize.Width = Math.Min(DesiredSize.Width, finalSizeWithoutMargins.Width);
            if (float.IsNaN(elementSize.Height))
                elementSize.Height = Math.Min(DesiredSize.Height, finalSizeWithoutMargins.Height);

            // clamp the element size between the maximum and minimum width/height of the UIElement
            elementSize = new Size2F(
                Math.Max(MinimumWidth, Math.Min(MaximumWidth, elementSize.Width)),
                Math.Max(MinimumHeight, Math.Min(MaximumHeight, elementSize.Height)));

            // let ArrangeOverride decide of the final taken size
            elementSize = ArrangeOverride(elementSize);

            // compute the rendering offsets
            var renderOffsets = CalculateAdjustmentOffsets(ref MarginInternal, ref finalSizeWithMargins, ref elementSize);

            // update UIElement internal variables
            RenderSize = elementSize;
            RenderOffsets = new Vector3(renderOffsets.X, renderOffsets.Y, -DepthOffset); // invert depthOffset because -z is forward but users expect positive offset to be towards camera.
        }

        private void ValidateChildrenArrange()
        {
            foreach (var child in VisualChildrenCollection)
            {
                if (!child.IsArrangeValid)
                {
                    child.IsArrangeValid = true;
                    child.ValidateChildrenArrange();
                }
            }
        }

        /// <summary>
        /// When overridden in a derived class, positions possible child elements and determines a size for a <see cref="UIElement"/> derived class.
        /// </summary>
        /// <param name="finalSizeWithoutMargins">The final area within the parent that this element should use to arrange itself and its children.</param>
        /// <returns>The actual size used.</returns>
        protected virtual Size2F ArrangeOverride(Size2F finalSizeWithoutMargins)
        {
            return finalSizeWithoutMargins;
        }

        /// <summary>
        /// When overridden in a derived class, collapse possible child elements and derived class.
        /// </summary>
        protected virtual void CollapseOverride()
        {
            DesiredSize = Size2F.Zero;
            DesiredSizeWithMargins = Size2F.Zero;
            RenderSize = Size2F.Zero;
            RenderOffsets = Vector3.Zero;

            foreach (var child in VisualChildrenCollection)
                PropagateCollapseToChild(child);
        }

        /// <summary>
        /// Propagate the collapsing to a child element <paramref name="element"/>.
        /// </summary>
        /// <param name="element">A child element to which propagate the collapse.</param>
        /// <exception cref="InvalidOperationException"><paramref name="element"/> is not a child of this element.</exception>
        protected void PropagateCollapseToChild(UIElement element)
        {
            if (element.VisualParent != this)
                throw new InvalidOperationException("Element is not a child of this element.");

            element.InvalidateMeasure();
            element.CollapseOverride();
        }

        /// <summary>
        /// Finds an element that has the provided identifier name in the element children.
        /// </summary>
        /// <param name="name">The name of the requested element.</param>
        /// <returns>The requested element. This can be null if no matching element was found.</returns>
        /// <remarks>If several elements with the same name exist return the first found</remarks>
        public UIElement FindName(string name)
        {
            if (Name == name)
                return this;

            return VisualChildren.Select(child => child.FindName(name)).FirstOrDefault(elt => elt != null);
        }

        /// <summary>
        /// Set the parent to a child.
        /// </summary>
        /// <param name="child">The child to which set the parent.</param>
        /// <param name="parent">The parent of the child.</param>
        protected static void SetParent([NotNull] UIElement child, [CanBeNull] UIElement parent)
        {
            if (parent != null && child.Parent != null && parent != child.Parent)
                throw new InvalidOperationException("The UI element 'Name="+child.Name+"' has already as parent the element 'Name="+child.Parent.Name+"'.");

            child.Parent = parent;
        }

        /// <summary>
        /// Set the visual parent to a child.
        /// </summary>
        /// <param name="child">The child to which set the visual parent.</param>
        /// <param name="parent">The parent of the child.</param>
        protected static void SetVisualParent([NotNull] UIElement child, [CanBeNull] UIElement parent)
        {
            if (child == null) throw new ArgumentNullException(nameof(child));
            if (parent != null && child.VisualParent != null && parent != child.VisualParent)
                throw new InvalidOperationException("The UI element 'Name=" + child.Name + "' has already as visual parent the element 'Name=" + child.VisualParent.Name + "'.");

            child.VisualParent?.VisualChildrenCollection.Remove(child);

            child.VisualParent = parent;

            if (parent != null)
            {
                child.LayoutingContext = parent.layoutingContext;
                parent.VisualChildrenCollection.Add(child);
            }
        }

        /// <summary>
        /// Calculate the intersection of the UI element and the ray.
        /// </summary>
        /// <param name="ray">The ray in world space coordinate</param>
        /// <param name="intersectionPoint">The intersection point in world space coordinate</param>
        /// <returns><value>true</value> if the two elements intersects, <value>false</value> otherwise</returns>
        protected internal virtual bool Intersects(ref Ray ray, out Vector3 intersectionPoint)
        {
            var renderSize = new Vector3(RenderSizeInternal.Width, RenderSizeInternal.Height, 0);
            // does ray intersect element Oxy face?
            var intersects = CollisionHelper.RayIntersectsRectangle(ref ray, ref WorldMatrixPickingInternal, ref renderSize, 2, out intersectionPoint);

            // if element has depth also test other faces
            if (WorldMatrix.TranslationVector.Z > MathUtil.ZeroTolerance)
            {
                Vector3 intersection;
                if (CollisionHelper.RayIntersectsRectangle(ref ray, ref WorldMatrixPickingInternal, ref renderSize, 0, out intersection))
                {
                    intersects = true;
                    if (intersection.Z > intersectionPoint.Z)
                        intersectionPoint = intersection;
                }
                if (CollisionHelper.RayIntersectsRectangle(ref ray, ref WorldMatrixPickingInternal, ref renderSize, 1, out intersection))
                {
                    intersects = true;
                    if (intersection.Z > intersectionPoint.Z)
                        intersectionPoint = intersection;
                }
            }

            return intersects;
        }

        void IUIElementUpdate.Update(GameTime time)
        {
            Update(time);

            foreach (var child in VisualChildrenCollection)
                ((IUIElementUpdate)child).Update(time);
        }

        void IUIElementUpdate.UpdateWorldMatrix(ref Matrix parentWorldMatrix, bool parentWorldChanged)
        {
            UpdateWorldMatrix(ref parentWorldMatrix, parentWorldChanged);
        }

        void IUIElementUpdate.UpdateElementState(int elementBias)
        {
            var parent = VisualParent;
            var parentRenderOpacity = 1f;
            var parentIsHierarchyEnabled = true;

            if (parent != null)
            {
                parentRenderOpacity = parent.RenderOpacity;
                parentIsHierarchyEnabled = parent.IsHierarchyEnabled;
            }

            RenderOpacity = parentRenderOpacity * Opacity;
            isHierarchyEnabled = parentIsHierarchyEnabled && isEnabled;
            DepthBias = elementBias;

            var currentElementDepthBias = DepthBias + DrawLayerNumber;

            foreach (var visualChild in VisualChildrenCollection)
            {
                ((IUIElementUpdate)visualChild).UpdateElementState(currentElementDepthBias);

                currentElementDepthBias = visualChild.MaxChildrenDepthBias + (visualChild.ClipToBounds ? visualChild.DrawLayerNumber : 0);
            }

            MaxChildrenDepthBias = currentElementDepthBias;
        }

        /// <summary>
        /// Method called by <see cref="IUIElementUpdate.Update"/>.
        /// This method can be overridden by inherited classes to perform time-based actions.
        /// This method is not in charge to recursively call the update on child elements, this is automatically done.
        /// </summary>
        /// <param name="time">The current time of the game</param>
        protected virtual void Update(GameTime time)
        {
            if (Parent != null && !Parent.UIElementServices.Equals(ref uiElementServices))
                uiElementServices = Parent.UIElementServices;
        }

        /// <summary>
        /// Method called by <see cref="IUIElementUpdate.UpdateWorldMatrix"/>.
        /// Parents are in charge of recursively calling this function on their children.
        /// </summary>
        /// <param name="parentWorldMatrix">The world matrix of the parent.</param>
        /// <param name="parentWorldChanged">Boolean indicating if the world matrix provided by the parent changed</param>
        protected virtual void UpdateWorldMatrix(ref Matrix parentWorldMatrix, bool parentWorldChanged)
        {
            if (parentWorldChanged || LocalMatrixChanged || ArrangeChanged)
            {
                var localMatrixCopy = localMatrix;

                // include rendering offsets into the local matrix.
                localMatrixCopy.TranslationVector += RenderOffsets + new Vector3(RenderSize.Width, RenderSize.Height, 0) / 2;

                // calculate the world matrix of UIElement
                Matrix worldMatrix;
                Matrix.Multiply(ref localMatrixCopy, ref parentWorldMatrix, out worldMatrix);
                WorldMatrix = worldMatrix;

                // Picking (see XK-4689) - this fix relates to the inverted axis introduced in
                //  UIRenderFeature.PickingUpdate(RenderUIElement renderUIElement, Viewport viewport, ref Matrix worldViewProj, GameTime drawTime)
                localMatrixCopy.M13 *= -1;
                localMatrixCopy.M31 *= -1;
                localMatrixCopy.M23 *= -1;
                localMatrixCopy.M32 *= -1;
                Matrix.Multiply(ref localMatrixCopy, ref parentWorldMatrix, out worldMatrix);
                WorldMatrixPickingInternal = worldMatrix;

                LocalMatrixChanged = false;
                ArrangeChanged = false;
            }
        }

        /// <summary>
        /// Computes the (X,Y) offsets to position correctly the UI element given the total provided space to it.
        /// </summary>
        /// <param name="thickness">The thickness around the element to position.</param>
        /// <param name="providedSpace">The total space given to the child element by the parent</param>
        /// <param name="usedSpaceWithoutThickness">The space used by the child element without the thickness included in it.</param>
        /// <returns>The offsets</returns>
        protected Vector2 CalculateAdjustmentOffsets(ref Thickness thickness, ref Size2F providedSpace, ref Size2F usedSpaceWithoutThickness)
        {
            // compute the size of the element with the thickness included
            var usedSpaceWithThickness = usedSpaceWithoutThickness + thickness;

            // set offset for left and stretch alignments
            var offsets = new Vector2(thickness.Left, thickness.Top);

            // align the element horizontally
            switch (HorizontalAlignment)
            {
                case HorizontalAlignment.Center:
                case HorizontalAlignment.Stretch:
                    offsets.X += (providedSpace.Width - usedSpaceWithThickness.Width) / 2;
                    break;
                case HorizontalAlignment.Right:
                    offsets.X += providedSpace.Width - usedSpaceWithThickness.Width;
                    break;
            }

            // align the element vertically
            switch (VerticalAlignment)
            {
                case VerticalAlignment.Center:
                case VerticalAlignment.Stretch:
                    offsets.Y += (providedSpace.Height - usedSpaceWithThickness.Height) / 2;
                    break;
                case VerticalAlignment.Bottom:
                    offsets.Y += providedSpace.Height - usedSpaceWithThickness.Height;
                    break;
            }

            return offsets;
        }
    }
}
