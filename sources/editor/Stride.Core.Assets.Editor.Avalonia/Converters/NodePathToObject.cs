// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Avalonia;
using Stride.Core.Presentation.Avalonia.Converters;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.Avalonia.Converters;

public sealed class NodePathToObject : OneWayValueConverter<NodePathToObject>
{
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not NodeViewModel node)
            return AvaloniaProperty.UnsetValue;

        if (parameter is not string pathParameter)
            return AvaloniaProperty.UnsetValue;

        var paths = pathParameter.Split('.', StringSplitOptions.RemoveEmptyEntries);
        object? current = node;
        int i = 0;
        for (; i < paths.Length; i++)
        {
            var p = paths[i];
            current = p[0] is '[' && p[^1] is ']'
                ? GetDynamic(current, p[1..^1])
                : GetProperty(current, p);
            if (current is null)
                break;
        }

        return i == paths.Length ? current : AvaloniaProperty.UnsetValue;

        static object? GetDynamic(object? obj, string name)
        {
            return obj is NodeViewModel node ? node[name] : null;
        }

        static object? GetProperty(object? obj, string name)
        {
            if (obj is not NodeViewModel node)
                return null;

            return name switch
            {
                nameof(NodeViewModel.DisplayName) => node.DisplayName,
                nameof(NodeViewModel.DisplayPath) => node.DisplayPath,
                nameof(NodeViewModel.HasCollection) => node.HasCollection,
                nameof(NodeViewModel.HasDictionary) => node.HasDictionary,
                nameof(NodeViewModel.HasList) => node.HasList,
                nameof(NodeViewModel.HasSet) => node.HasSet,
                nameof(NodeViewModel.IsReadOnly) => node.IsReadOnly,
                nameof(NodeViewModel.IsVisible) => node.IsVisible,
                nameof(NodeViewModel.Name) => node.Name,
                nameof(NodeViewModel.NodeValue) => node.NodeValue,
                nameof(NodeViewModel.Root) => node.Root,
                nameof(NodeViewModel.VisibleChildrenCount) => node.VisibleChildrenCount,
                _ => throw new ArgumentException($"Unsupported {name} property.")
            };
        }
    }
}
