// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Assets.Editor.Avalonia.Views;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Rendering.Materials.ComputeColors;

namespace Stride.Assets.Editor.Avalonia.Views;

/// <summary>
/// Matches the shader name of a compute shader class of a material (a color or a scalar computed by an effect shader).
/// </summary>
public sealed class ShaderClassNodeMixinReferenceTemplateProvider : NodeViewModelTemplateProvider
{
    public override string Name => "ShaderClassNodeMixinReference";

    public override bool MatchNode(NodeViewModel node)
    {
        return node.Parent?.NodeValue is ComputeShaderClassColor or ComputeShaderClassScalar && node.Type == typeof(string);
    }
}
