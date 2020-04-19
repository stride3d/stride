// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

using Stride.Core.Reflection;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.View;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.View.TemplateProviders
{
    public class NullableTemplateProvider : NodeViewModelTemplateProvider
    {
        public override string Name => $"Nullable{(Struct ? "Struct" : "")}";

        public bool Struct { get; set; }

        public override bool MatchNode(NodeViewModel node)
        {
            if (Struct)
            {
                var underlyingType = Nullable.GetUnderlyingType(node.Type);
                return underlyingType != null && underlyingType.IsStruct();
            }

            // interfaces are not strictly nullable, but they are abstract.
            return node.Type.IsNullable() || node.Type.IsAbstract;
        }
    }
}
