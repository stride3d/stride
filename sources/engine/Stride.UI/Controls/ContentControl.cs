// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

using Stride.Core;
using Stride.Core.Mathematics;

namespace Stride.UI.Controls
{
    /// <summary>
    /// Represents a control with a single piece of content of any type.
    /// </summary>
    [DataContract(nameof(ContentControl))]
    [DebuggerDisplay("ContentControl - Name={Name}")]
    public abstract class ContentControl : Control
    {
        private UIElement content;

        private UIElement visualContent;

        /// <summary>
        /// The key to the ContentArrangeMatrix dependency property.
        /// </summary>
        protected static readonly PropertyKey<Matrix> ContentArrangeMatrixPropertyKey = DependencyPropertyFactory.RegisterAttached(nameof(ContentArrangeMatrixPropertyKey), typeof(ContentControl), Matrix.Identity);

        private Matrix contentWorldMatrix;

        /// <summary>
        /// Gets or sets the content of the ContentControl.
        /// </summary>
        /// <exception cref="InvalidOperationException">The value passed has already a parent.</exception>
        /// <userdoc>The content of the Content Control.</userdoc>
        [DataMember]
        [DefaultValue(null)]
        public virtual UIElement Content
        {
            get { return content; }
            set
            {
                if (content == value)
                    return;

                if (content != null)
                    SetParent(content, null);

                content = value;
                VisualContent = content;

                if (content != null)
                    SetParent(content, this);

                InvalidateMeasure();
            }
        }

        /// <summary>
        /// Gets the visual content of the ContentControl.
        /// </summary>
        [DataMemberIgnore]
        public UIElement VisualContent
        {
            get { return visualContent; }
            protected set
            {
                if (VisualContent != null)
                    SetVisualParent(VisualContent, null);

                visualContent = value;

                if (VisualContent != null)
                    SetVisualParent(visualContent, this);

                InvalidateMeasure();
            }
        }

        /// <inheritdoc/>
        protected override IEnumerable<IUIElementChildren> EnumerateChildren()
        {
            if (Content != null)
                yield return Content;
        }

        protected override Vector3 MeasureOverride(Vector3 availableSizeWithoutMargins)
        {
            // measure size desired by the children
            var childDesiredSizeWithMargins = Vector3.Zero;
            if (VisualContent != null)
            {
                // remove space for padding in availableSizeWithoutMargins
                var childAvailableSizeWithMargins = CalculateSizeWithoutThickness(ref availableSizeWithoutMargins, ref padding);

                VisualContent.Measure(childAvailableSizeWithMargins);
                childDesiredSizeWithMargins = VisualContent.DesiredSizeWithMargins;
            }

            // add the padding to the child desired size
            var desiredSizeWithPadding = CalculateSizeWithThickness(ref childDesiredSizeWithMargins, ref padding);

            return desiredSizeWithPadding;
        }

        protected override Vector3 ArrangeOverride(Vector3 finalSizeWithoutMargins)
        {
            // arrange the content
            if (VisualContent != null)
            {
                // calculate the remaining space for the child after having removed the padding space.
                var childSizeWithoutPadding = CalculateSizeWithoutThickness(ref finalSizeWithoutMargins, ref padding);

                // arrange the child
                VisualContent.Arrange(childSizeWithoutPadding, IsCollapsed);

                // compute the rendering offsets of the child element wrt the parent origin (0,0,0)
                var childOffsets = new Vector3(Padding.Left, Padding.Top, Padding.Front) - finalSizeWithoutMargins / 2;

                // set the arrange matrix of the child.
                VisualContent.DependencyProperties.Set(ContentArrangeMatrixPropertyKey, Matrix.Translation(childOffsets));
            }

            return finalSizeWithoutMargins;
        }

        protected override void UpdateWorldMatrix(ref Matrix parentWorldMatrix, bool parentWorldChanged)
        {
            var contentMatrixChanged = parentWorldChanged || ArrangeChanged || LocalMatrixChanged;

            base.UpdateWorldMatrix(ref parentWorldMatrix, parentWorldChanged);

            if (VisualContent != null)
            {
                if (contentMatrixChanged)
                {
                    var contentMatrix = VisualContent.DependencyProperties.Get(ContentArrangeMatrixPropertyKey);
                    Matrix.Multiply(ref contentMatrix, ref WorldMatrixInternal, out contentWorldMatrix);
                }

                ((IUIElementUpdate)VisualContent).UpdateWorldMatrix(ref contentWorldMatrix, contentMatrixChanged);
            }
        }
    }
}
