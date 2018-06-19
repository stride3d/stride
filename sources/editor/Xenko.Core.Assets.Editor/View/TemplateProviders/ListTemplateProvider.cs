// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.View;
using Xenko.Core.Presentation.Quantum.ViewModels;

namespace Xenko.Core.Assets.Editor.View.TemplateProviders
{
    public class ListTemplateProvider : NodeViewModelTemplateProvider
    {
        public override string Name => "List" + (ElementType?.Name ?? "");

        public Type ElementType { get; set; }

        public override bool MatchNode(NodeViewModel node)
        {
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
            return node.HasCollection && !node.HasDictionary && node.NodeValue != null && matchElementType;
        }
    }
}
