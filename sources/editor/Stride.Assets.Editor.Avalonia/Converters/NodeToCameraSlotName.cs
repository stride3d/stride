// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Globalization;
using Stride.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Stride.Core.Presentation.Avalonia.Converters;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Translation;
using Stride.Rendering.Compositing;

namespace Stride.Assets.Editor.Avalonia.Converters;

/// <summary>
/// Converts the camera slot of a camera component (the first value, the slot or its node) to the name of the slot among the
/// slots of the graphics compositors (the second value, the entries of the slot list).
/// </summary>
public sealed class NodeToCameraSlotName : MultiValueConverterBase<NodeToCameraSlotName>
{
    /// <inheritdoc/>
    public override object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        // The node itself: changing the slot refreshes the node, which gives new entries, but does not always notify a change of
        // its value
        var value = values.Count > 0 && values[0] is NodeViewModel node ? node.NodeValue : values.ElementAtOrDefault(0);
        if (values.Count < 2 || value is not SceneCameraSlotId slotId)
            return null;

        if (values[1] is IEnumerable<AbstractNodeEntry> slots)
        {
            foreach (var slot in slots.OfType<AbstractNodeValue>())
            {
                if (slot.Value is SceneCameraSlotId id && id.Id == slotId.Id)
                    return slot.DisplayValue;
            }
        }

        return Tr._p("Property", "(Invalid camera slot)");
    }
}
