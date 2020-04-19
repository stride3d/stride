// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Windows;

using Stride.Core.Presentation.Behaviors;
using Stride.Core.Presentation.Quantum;
using Stride.Core.Presentation.Quantum.ViewModels;

namespace Stride.Core.Assets.Editor.View.Behaviors
{
    public class PropertyViewFilteringBehavior : ItemsControlCollectionViewBehavior
    {
        public static readonly DependencyProperty FilterTokenProperty = DependencyProperty.Register("FilterToken", typeof(string), typeof(PropertyViewFilteringBehavior), new PropertyMetadata(null, FilterTokenChanged));

        public string FilterToken { get { return (string)GetValue(FilterTokenProperty); } set { SetValue(FilterTokenProperty, value); } }

        private static void FilterTokenChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (PropertyViewFilteringBehavior)d;
            var token = behavior.FilterToken;
            if (!string.IsNullOrWhiteSpace(token))
                behavior.FilterPredicate = x => Match((NodeViewModel)x, token);
            else
                behavior.FilterPredicate = null;
        }

        private static bool Match(NodeViewModel node, string token)
        {
            return node.DisplayName.IndexOf(token, StringComparison.CurrentCultureIgnoreCase) >= 0 || node.Children.Any(x => Match(x, token));
        }
    }
}
