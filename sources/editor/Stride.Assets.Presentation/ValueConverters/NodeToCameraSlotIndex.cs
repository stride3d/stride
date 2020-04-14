// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Xenko.Core.Assets.Editor.Quantum.NodePresenters.Commands;
using Xenko.Rendering.Compositing;
using Xenko.Core.Presentation.ValueConverters;

namespace Xenko.Assets.Presentation.ValueConverters
{
    public class NodeToCameraSlotIndex : OneWayMultiValueConverter<NodeToCameraSlotIndex>
    {
        /// <inheritdoc/>
        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var slotIndex = (SceneCameraSlotId)values[0];

            var slots = values[1] as IList<AbstractNodeEntry>;
            if (slots != null)
            {
                foreach (var slot in slots.OfType<AbstractNodeValue>().Where(x => x.Value is SceneCameraSlotId))
                {
                    if (((SceneCameraSlotId)slot.Value).Id == slotIndex.Id)
                        return slot.DisplayValue;
                }
            }

            return "(Invalid camera index)";
        }
    }
}
