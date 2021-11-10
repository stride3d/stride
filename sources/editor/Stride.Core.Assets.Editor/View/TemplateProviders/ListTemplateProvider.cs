// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.View;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.View.TemplateProviders
{
    public class ListTemplateProvider : NodeViewModelTemplateProvider
    {
        public override string Name => "List" + (ElementType?.Name ?? "");

        public Type ElementType { get; set; }

        public override bool MatchNode(NodeViewModel node)
        {
            if (node.HasList)
            {
                return node.NodeValue != null;
            }

            var matchElementType = ElementType == null;
            if (!matchElementType)
            {
                var listType = node.Type;
                if (listType.IsGenericType)
                {
                    var genParam = listType.GetGenericArguments();
                    matchElementType = genParam.Length == 1 && genParam[0] == ElementType;
                }
            }
            return node.HasCollection && !node.HasSet && !node.HasDictionary && node.NodeValue != null && matchElementType;
        }
    }
}
