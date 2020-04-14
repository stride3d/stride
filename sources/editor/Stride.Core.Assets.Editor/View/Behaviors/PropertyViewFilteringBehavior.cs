// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Linq;
using System.Windows;

using Xenko.Core.Presentation.Behaviors;
using Xenko.Core.Presentation.Quantum;
using Xenko.Core.Presentation.Quantum.ViewModels;

namespace Xenko.Core.Assets.Editor.View.Behaviors
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
