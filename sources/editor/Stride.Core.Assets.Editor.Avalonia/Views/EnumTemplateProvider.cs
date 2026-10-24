// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Views;

public sealed class EnumTemplateProvider : NodeViewModelTemplateProvider
{
    public override string Name  => $"{(ImageEnum ? "Image" : "")}{(FlagEnum ? "Flag" : "")}Enum";

    public bool ImageEnum { get; set; }

    public bool FlagEnum { get; set; }

    public override bool MatchNode(NodeViewModel node)
    {
        var isEnum = TypeIsEnum(node.Type) || (node.NodeValue is not null && TypeIsEnum(node.NodeValue.GetType()));
        if (!isEnum)
            return false;

        if (FlagEnum)
        {
            if (node.Type.GetCustomAttributes(typeof(FlagsAttribute), false).Length == 0)
                return false;
        }

        if (ImageEnum)
        {
            // FIXME xplat-editor
            return false;
        }

        return true;
    }

    private static bool TypeIsEnum(Type type)
    {
        return type.IsEnum || Nullable.GetUnderlyingType(type) is { IsEnum: true };
    }
}
