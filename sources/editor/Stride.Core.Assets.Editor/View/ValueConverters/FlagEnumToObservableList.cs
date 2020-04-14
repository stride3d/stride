// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using Xenko.Core.Extensions;
using Xenko.Core.Presentation.Collections;
using Xenko.Core.Presentation.Extensions;
using Xenko.Core.Presentation.Quantum.ViewModels;
using Xenko.Core.Presentation.ValueConverters;

namespace Xenko.Core.Assets.Editor.View.ValueConverters
{
    public class FlagEnumToObservableList : OneWayMultiValueConverter<FlagEnumToObservableList>
    {
        bool updatingCollection;

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
                var result = enumValue.GetIndividualFlags();
                collection.AddRange(result);
            }
            collection.CollectionChanged += (sender, e) => CollectionChanged(collection, node, e);
            return collection;
        }

        private void CollectionChanged(ObservableList<object> collection, NodeViewModel node, NotifyCollectionChangedEventArgs e)
        {
            if (updatingCollection)
                return;

            updatingCollection = true;

            var newEnumValue = EnumExtensions.GetEnum(node.Type, collection.Cast<Enum>());

            if (e.OldItems != null)
            {
                foreach (var oldItem in e.OldItems)
                {
                    newEnumValue = (Enum)Enum.ToObject(node.Type, System.Convert.ToUInt64(newEnumValue) & ~System.Convert.ToUInt64(oldItem));
                }
            }
            if (e.NewItems != null)
            {
                foreach (var oldItem in e.NewItems)
                {
                    newEnumValue = (Enum)Enum.ToObject(node.Type, System.Convert.ToUInt64(newEnumValue) | System.Convert.ToUInt64(oldItem));
                }
            }

            var flags = newEnumValue.GetAllFlags().ToList();
            foreach (var item in collection.ToList())
            {
                if (!flags.Contains(item))
                    collection.Remove(item);
            }

            foreach (var flag in flags)
            {
                if (!collection.Contains(flag))
                    collection.Add(flag);
            }

            if (!Equals(node.NodeValue, EnumExtensions.GetEnum(node.Type, collection.Cast<Enum>())))
            {
                node.NodeValue = EnumExtensions.GetEnum(node.Type, collection.Cast<Enum>());
            }

            updatingCollection = false;
        }
    }
}
