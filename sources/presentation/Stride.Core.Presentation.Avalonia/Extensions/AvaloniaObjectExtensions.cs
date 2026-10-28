// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Avalonia;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using VisualExtensions = Avalonia.VisualTree.VisualExtensions;

namespace Stride.Core.Presentation.Avalonia.Extensions;

public static class AvaloniaObjectExtensions
{
    public static T? FindLogicalChildOfType<T>(this AvaloniaObject source)
        where T : class, ILogical
    {
        ArgumentNullException.ThrowIfNull(source);
        return (source as ILogical)?.FindLogicalDescendantOfType<T>();
    }
    
    public static IEnumerable<T> FindLogicalChildrenOfType<T>(this AvaloniaObject source)
        where T : class, ILogical
    {
        ArgumentNullException.ThrowIfNull(source);
        return source is ILogical logical
            ? logical.GetLogicalDescendants().OfType<T>()
            : [];
    }

    public static T? FindLogicalParentOfType<T>(this AvaloniaObject source)
        where T : class, ILogical
    {
        ArgumentNullException.ThrowIfNull(source);
        return (source as ILogical)?.FindLogicalAncestorOfType<T>();
    }

    public static T? FindVisualChildOfType<T>(this AvaloniaObject source)
        where T : Visual
    {
        ArgumentNullException.ThrowIfNull(source);
        return (source as Visual)?.FindDescendantOfType<T>();
    }

    public static IEnumerable<T> FindVisualChildrenOfType<T>(this AvaloniaObject source)
        where T : Visual
    {
        ArgumentNullException.ThrowIfNull(source);
        return source is Visual visual
            ? visual.GetVisualDescendants().OfType<T>()
            : [];
    }

    public static T? FindVisualParentOfType<T>(this AvaloniaObject source)
        where T : Visual
    {
        ArgumentNullException.ThrowIfNull(source);
        return (source as Visual)?.FindAncestorOfType<T>();
    }

    public static Visual? FindVisualRoot(this AvaloniaObject source)
    {
        ArgumentNullException.ThrowIfNull(source);

        Visual? root = null;
        var current = source as Visual;
        while (current is not null)
        {
            root = current;
            current = VisualExtensions.GetVisualParent(current);
        }

        return root;
    }
}
