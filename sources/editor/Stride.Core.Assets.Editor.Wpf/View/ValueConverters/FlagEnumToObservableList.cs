// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using Stride.Core.Extensions;
using Stride.Core.Presentation.Collections;
using Stride.Core.Presentation.Quantum.ViewModels;
using Stride.Core.Presentation.ValueConverters;

namespace Stride.Core.Assets.Editor.View.ValueConverters
{
    public class FlagEnumToObservableList : OneWayMultiValueConverter<FlagEnumToObservableList>
    {
        // Note: this converter is a multi-converter so we can make a multi-binding that also binds the value of the node, triggering an update when the value changes.

        public override object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            var node = (NodeViewModel)values[0];
            if (node == null)
                return Enumerable.Empty<object>();

            var value = values[1];
            var collection = new ObservableList<object>();
            if (value != null)
            {
                var enumValue = (Enum)value;
                collection.AddRange(enumValue.GetAllFlags());
            }
            collection.CollectionChanged += (sender, e) => CollectionChanged(collection, node, e);
            return collection;
        }

        private static void CollectionChanged(ObservableList<object> collection, NodeViewModel node, NotifyCollectionChangedEventArgs e)
        {
            // The collection must not change during its own event: the new node value rebuilds it through the multi-binding.
            var newEnumValue = EnumExtensions.GetEnum(node.Type, collection.Cast<Enum>());

            // A removed combination clears all its bits, also the ones of the individual flags still in the collection.
            if (e.OldItems != null)
            {
                foreach (var oldItem in e.OldItems)
                {
                    newEnumValue = (Enum)Enum.ToObject(node.Type, System.Convert.ToUInt64(newEnumValue) & ~System.Convert.ToUInt64(oldItem));
                }
            }
            if (e.NewItems != null)
            {
                foreach (var newItem in e.NewItems)
                {
                    newEnumValue = (Enum)Enum.ToObject(node.Type, System.Convert.ToUInt64(newEnumValue) | System.Convert.ToUInt64(newItem));
                }
            }

            if (!Equals(node.NodeValue, newEnumValue))
            {
                node.NodeValue = newEnumValue;
            }
        }
    }
}
