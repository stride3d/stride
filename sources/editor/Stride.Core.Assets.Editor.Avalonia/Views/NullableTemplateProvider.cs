// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Reflection;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

public sealed class NullableTemplateProvider : NodeViewModelTemplateProvider
{
    public override string Name => "Nullable";

    public override bool MatchNode(NodeViewModel node)
    {
        var underlyingType = Nullable.GetUnderlyingType(node.Type);
        return underlyingType is not null && underlyingType.IsStruct();
    }
}
