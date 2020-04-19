// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Stride.Core.Assets;
using Stride.Core.Assets.Serializers;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Extensions;
using Stride.Core.Mathematics;
using Stride.Core.Yaml;
using Stride.Core.Yaml.Serialization;
using Stride.Engine;
using Stride.UI;

namespace Stride.Assets.UI
{
    /// <summary>
    /// Base class for assets containing a hierarchy of <see cref="UIElement"/>.
    /// </summary>
    public abstract class UIAssetBase : AssetCompositeHierarchy<UIElementDesign, UIElement>
    {
        [DataContract("UIDesign")]
        public sealed class UIDesign
        {
            [DataMember]
            [Display(category: "Design")]
            public Vector3 Resolution { get; set; } = new Vector3(UIComponent.DefaultWidth, UIComponent.DefaultHeight, UIComponent.DefaultDepth);
        }

        [DataMember(10)]
        [NotNull]
        public UIDesign Design { get; set; } = new UIDesign();

        /// <inheritdoc/>
        public override UIElement GetParent(UIElement part)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            return part.VisualParent;
        }

        /// <inheritdoc/>
        public override int IndexOf(UIElement part)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            var parent = GetParent(part);
            return parent?.VisualChildren.IndexOf(x => x == part) ?? Hierarchy.RootParts.IndexOf(part);
        }

        /// <inheritdoc/>
        public override UIElement GetChild(UIElement part, int index)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            return part.VisualChildren[index];
        }

        /// <inheritdoc/>
        public override int GetChildCount(UIElement part)
        {
            if (part == null) throw new ArgumentNullException(nameof(part));
            return part.VisualChildren.Count;
        }

        /// <inheritdoc/>
        public override IEnumerable<UIElement> EnumerateChildParts(UIElement part, bool isRecursive)
        {
            var elementChildren = (IUIElementChildren)part;
            var enumerator = isRecursive ? elementChildren.Children.DepthFirst(t => t.Children) : elementChildren.Children;
            return enumerator.NotNull().Cast<UIElement>();
        }
    }
}
