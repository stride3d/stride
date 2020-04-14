// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using Stride.Core.IO;

namespace Stride.Core.Assets.Editor.View.Behaviors
{
    internal class DragDropHelper
    {
        internal static DragContainer GetDragContainer(IDataObject dataObject)
        {
            var dragContainer = dataObject.GetData(DragContainer.Format) as DragContainer;
            return dragContainer;
        }

        internal static IReadOnlyCollection<object> GetItemsToDrop(DragContainer dragContainer, IDataObject dataObject)
        {
            if (dragContainer != null)
            {
                // We're using internal drag'n'drop system.
                return dragContainer.Items;
            }

            if (dataObject == null)
                return null;

            if (dataObject.GetDataPresent(DataFormats.FileDrop))
            {
                // Drag data come from outside the application.
                var fileStrings = (string[])dataObject.GetData(DataFormats.FileDrop);
                return fileStrings.Where(UFile.IsValid).Select(x => new UFile(x)).Cast<object>().ToArray();
            }

            if (dataObject.GetDataPresent(DataFormats.Serializable))
            {
                var data = (object[])dataObject.GetData(DataFormats.Serializable);
                return data;
            }

            return null;
        }

        internal static bool ShouldDisplayDropAdorner(DisplayDropAdorner rule, IEnumerable<object> itemsToDrop)
        {
            switch (rule)
            {
                case DisplayDropAdorner.Never:
                    return false;
                case DisplayDropAdorner.InternalOnly:
                    return itemsToDrop.All(x => !(x is UFile));
                case DisplayDropAdorner.ExternalOnly:
                    return itemsToDrop.All(x => x is UFile);
                case DisplayDropAdorner.Always:
                    return true;
                default:
                    throw new ArgumentOutOfRangeException(nameof(rule));
            }
        }
    }
}
