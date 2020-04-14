// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;

using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Collections;
using Stride.Core.Mathematics;
using Stride.UI.Controls;

namespace Stride.UI.Panels
{
    /// <summary>
    /// Provides a base class for all Panel elements. Use Panel elements to position and arrange child objects Stride applications.
    /// </summary>
    [DataContract(nameof(Panel))]
    [DebuggerDisplay("Panel - Name={Name}")]
    [Display(category: PanelCategory)]
    public abstract class Panel : UIElement, IScrollAnchorInfo
    {
        /// <summary>
        /// The key to the ZIndex dependency property.
        /// </summary>
        [Display(category: AppearanceCategory)]
        public static readonly PropertyKey<int> ZIndexPropertyKey = DependencyPropertyFactory.RegisterAttached(nameof(ZIndexPropertyKey), typeof(Panel), 0, PanelZSortedChildInvalidator);

        /// <summary>
        /// The key to the PanelArrangeMatrix dependency property. This property can be used by panels to arrange they children as they want.
        /// </summary>
        protected static readonly PropertyKey<Matrix> PanelArrangeMatrixPropertyKey = DependencyPropertyFactory.RegisterAttached(nameof(PanelArrangeMatrixPropertyKey), typeof(Panel), Matrix.Identity, InvalidateArrangeMatrix);

        private static void InvalidateArrangeMatrix(object propertyOwner, PropertyKey<Matrix> propertyKey, Matrix propertyOldValue)
        {
            var element = (UIElement)propertyOwner;
            var parentPanel = element.VisualParent as Panel;
            // if the element is not added to a panel yet, the invalidation will occur during the add of the child
            parentPanel?.childrenWithArrangeMatrixInvalidated.Add(element);
        }

        private readonly bool[] shouldAnchor = new bool[3];

        /// <summary>
        /// A comparer sorting the <see cref="Panel"/> children by increasing Z-Index.
        /// </summary>
        protected class PanelChildrenComparer : Comparer<UIElement>
        {
            public override int Compare(UIElement x, UIElement y)
            {
                if (x == y)
                    return 0;

                if (x == null)
                    return 1;

                if (y == null)
                    return -1;

                return x.GetPanelZIndex() - y.GetPanelZIndex();
            }
        }
        /// <summary>
        /// A instance of <see cref="PanelChildrenComparer"/> that can be used to sort panels children by increasing Z-Indices.
        /// </summary>
        protected static readonly PanelChildrenComparer PanelChildrenSorter = new PanelChildrenComparer();

        private readonly HashSet<UIElement> childrenWithArrangeMatrixInvalidated = new HashSet<UIElement>();
        private Matrix[] childrenArrangeWorldMatrix = new Matrix[2];

        /// <summary>
        /// Gets the <see cref="UIElementCollection"/> of child elements of this Panel.
        /// </summary>
        [DataMember(DataMemberMode.Content)]
        [MemberCollection(CanReorderItems = true, NotNullItems = true)]
        public UIElementCollection Children { get; }

        /// <inheritdoc/>
        protected override IEnumerable<IUIElementChildren> EnumerateChildren()
        {
            return Children;
        }

        /// <summary>
        /// Invalidation callback that sort panel children back after a modification of a child ZIndex.
        /// </summary>
        /// <param name="element">The element which had is ZIndex modified</param>
        /// <param name="key">The key of the modified property</param>
        /// <param name="oldValue">The value of the property before modification</param>
        private static void PanelZSortedChildInvalidator(object element, PropertyKey<int> key, int oldValue)
        {
            var uiElement = (UIElement)element;
            var parentAsPanel = uiElement.VisualParent as Panel;

            parentAsPanel?.VisualChildrenCollection.Sort(PanelChildrenSorter);
        }

        /// <summary>
        /// Creates a new empty Panel.
        /// </summary>
        protected Panel()
        {
            // activate anchoring by default
            for (var i = 0; i < shouldAnchor.Length; i++)
                shouldAnchor[i] = true;

            Children = new UIElementCollection();
            Children.CollectionChanged += LogicalChildrenChanged;
        }

        /// <summary>
        /// Action to take when the Children collection is modified.
        /// </summary>
        /// <param name="sender">Sender of the event</param>
        /// <param name="trackingCollectionChangedEventArgs">Argument indicating what changed in the collection</param>
        protected void LogicalChildrenChanged(object sender, TrackingCollectionChangedEventArgs trackingCollectionChangedEventArgs)
        {
            var modifiedElement = (UIElement)trackingCollectionChangedEventArgs.Item;
            var elementIndex = trackingCollectionChangedEventArgs.Index;
            switch (trackingCollectionChangedEventArgs.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    OnLogicalChildAdded(modifiedElement, elementIndex);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    OnLogicalChildRemoved(modifiedElement, elementIndex);
                    break;
                default:
                    throw new NotSupportedException();
            }
            InvalidateMeasure();
        }

        /// <summary>
        /// Action to perform when a logical child is removed.
        /// </summary>
        /// <param name="oldElement">The element that has been removed</param>
        /// <param name="index">The index of the child removed in the collection</param>
        protected virtual void OnLogicalChildRemoved(UIElement oldElement, int index)
        {
            if (oldElement.Parent == null)
                throw new UIInternalException("The parent of the removed children UIElement not null");
            SetParent(oldElement, null);
            SetVisualParent(oldElement, null);

            if (oldElement.MouseOverState != MouseOverState.MouseOverNone)
                MouseOverState = MouseOverState.MouseOverNone;
        }

        /// <summary>
        /// Action to perform when a logical child is added.
        /// </summary>
        /// <param name="newElement">The element that has been added</param>
        /// <param name="index">The index in the collection where the child has been added</param>
        protected virtual void OnLogicalChildAdded(UIElement newElement, int index)
        {
            if (newElement == null)
                throw new InvalidOperationException("Cannot add a null UIElement to the children list.");
            SetParent(newElement, this);
            SetVisualParent(newElement, this);
            VisualChildrenCollection.Sort(PanelChildrenSorter);
            if (Children.Count > childrenArrangeWorldMatrix.Length)
                childrenArrangeWorldMatrix = new Matrix[2 * Children.Count];
        }

        protected override void UpdateWorldMatrix(ref Matrix parentWorldMatrix, bool parentWorldChanged)
        {
            var shouldUpdateAllChridrenMatrix = parentWorldChanged || ArrangeChanged || LocalMatrixChanged;

            base.UpdateWorldMatrix(ref parentWorldMatrix, parentWorldChanged);

            var childIndex = 0;
            foreach (var child in VisualChildrenCollection)
            {
                var shouldUpdateChildWorldMatrix = shouldUpdateAllChridrenMatrix || childrenWithArrangeMatrixInvalidated.Contains(child);
                {
                    var childMatrix = child.DependencyProperties.Get(PanelArrangeMatrixPropertyKey);
                    Matrix.Multiply(ref childMatrix, ref WorldMatrixInternal, out childrenArrangeWorldMatrix[childIndex]);
                }

                ((IUIElementUpdate)child).UpdateWorldMatrix(ref childrenArrangeWorldMatrix[childIndex], shouldUpdateChildWorldMatrix);

                ++childIndex;
            }
            childrenWithArrangeMatrixInvalidated.Clear();
        }

        /// <summary>
        /// Change the anchoring activation state of the given direction.
        /// </summary>
        /// <param name="direction">The direction in which activate or deactivate the anchoring</param>
        /// <param name="enable"><value>true</value> to enable anchoring, <value>false</value> to disable the anchoring</param>
        public void ActivateAnchoring(Orientation direction, bool enable)
        {
            shouldAnchor[(int)direction] = enable;
        }

        public virtual bool ShouldAnchor(Orientation direction)
        {
            return shouldAnchor[(int)direction];
        }

        public virtual Vector2 GetSurroudingAnchorDistances(Orientation direction, float position)
        {
            var maxPosition = RenderSize[(int)direction];
            var validPosition = Math.Max(0, Math.Min(position, maxPosition));

            return new Vector2(-validPosition, maxPosition - validPosition);
        }

        [DataMemberIgnore]
        public ScrollViewer ScrollOwner { get; set; }
    }
}
