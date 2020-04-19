// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Windows;
using System.Windows.Controls;

using Stride.Core.Assets.Editor.ViewModel;

namespace Stride.Core.Assets.Editor.View
{
    public class TreeViewTemplateSelector : DataTemplateSelector
    {
        public DataTemplate AssetMountPointTemplate { get; set; }

        public DataTemplate DirectoryTemplate { get; set; }

        public DataTemplate PackageTemplate { get; set; }

        public DataTemplate DependencyCategoryTemplate { get; set; }

        public DataTemplate ProjectTemplate { get; set; }

        public DataTemplate ProjectCodeTemplate { get; set; }

        public DataTemplate PackageReferenceTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null)
            {
                return null;
            }
            if (item is ProjectViewModel)
            {
                return ProjectTemplate;
            }
            if (item is PackageViewModel)
            {
                return PackageTemplate;
            }
            if (item is DirectoryViewModel)
            {
                return DirectoryTemplate;
            }
            if (item is AssetMountPointViewModel)
            {
                return AssetMountPointTemplate;
            }
            if (item is DependencyCategoryViewModel)
            {
                return DependencyCategoryTemplate;
            }
            if (item is ProjectCodeViewModel)
            {
                return ProjectCodeTemplate;
            }
            if (item is PackageReferenceViewModel)
            {
                return PackageReferenceTemplate;
            }
            throw new ArgumentException("The type of item is unsupported.");
        }
    }
}
