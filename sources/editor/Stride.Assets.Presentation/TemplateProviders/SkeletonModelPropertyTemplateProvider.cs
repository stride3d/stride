// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;

using Xenko.Assets.Models;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.View;
using Xenko.Core.Presentation.Quantum.ViewModels;

namespace Xenko.Assets.Presentation.TemplateProviders
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
