// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

using Stride.Assets.Models;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.View;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Assets.Presentation.TemplateProviders
{
    public class SkeletonModelPropertyTemplateProvider : NodeViewModelTemplateProvider
    {
        public enum SkeletonProperty
        {
            Unset,
            NodeInformation,
            NodeInformationList,
        }

        public override string Name { get { return string.Format("Skeleton{0}", Property); } }

        public SkeletonProperty Property { get; set; }

        public override bool MatchNode(NodeViewModel node)
        {
            if (!typeof(SkeletonAsset).IsAssignableFrom(node.Root.Type))
                return false;

            if (node.Parent == null)
                return false;

            switch (Property)
            {
                case SkeletonProperty.NodeInformation:
                    return node.Type == typeof(NodeInformation);
                case SkeletonProperty.NodeInformationList:
                    return typeof(IEnumerable<NodeInformation>).IsAssignableFrom(node.Type);
                default:
                    throw new InvalidOperationException("Model property is unset.");
            }
        }
    }
}
