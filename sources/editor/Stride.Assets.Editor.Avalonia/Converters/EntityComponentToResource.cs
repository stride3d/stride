// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Stride.Assets.Presentation;
using Stride.Core.Assets.Editor.Avalonia.Converters;
using Stride.Core.Presentation.Avalonia.Converters;
using Stride.Engine;

namespace Stride.Assets.Editor.Avalonia.Converters;

/// <summary>
/// Converts the components of an entity to the icon of the entity in the hierarchy: the image of its component with the
/// highest order, as in the WPF scene editor.
/// </summary>
public sealed class EntityComponentToResource : OneWayValueConverter<EntityComponentToResource>
{
    /// <inheritdoc />
    public override object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not IEnumerable<EntityComponent> components)
            return null;

        var componentType = StrideDefaultAssetsPlugin.GetComponentsByOrder(components.Select(x => x.GetType()), false).FirstOrDefault();
        return TypeToResource.FetchResourceFromType(componentType, true);
    }
}
