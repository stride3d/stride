// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Xaml;

namespace Stride.Core.Presentation.Graph.Helper
{
    /// <summary>
    /// 
    /// </summary>
    public static class UIHelper
    {
        /// <summary>
        /// Finds a Child of a given item in the visual tree. 
        /// </summary>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="parent">Parent dependency object.</param>
        /// <param name="name">x:Name or Name of the child.</param>
        /// <returns></returns>
        public static T FindChild<T>(DependencyObject parent, string name)
            where T : DependencyObject
        {
            T result = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                var frameworkElement = child as FrameworkElement;
                if (frameworkElement != null)
                {
                    // Code to take care of Paragraph/Run
                    if (frameworkElement is TextBlock)
                    {
                        result = ((TextBlock)frameworkElement).FindName(name) as T;
                        if (result != null)
                        {
                            return result;
                        }
                    }

                    // If the child's name is the one that's being queried
                    if (frameworkElement.Name == name)
                    {
                        return frameworkElement as T;
                    }
                }

                // Recursively traverse the visual tree
                result = FindChild<T>(child, name);
                if (result != null)
                {
                    // If the child is found, break and return the results
                    break;
                }
            }

            return result;
        }

        /*
        // TODO Move somewhere else
        public static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            //get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            //we've reached the end of the tree
            if (parentObject == null) return null;

            //check if the parent matches the type we're looking for
            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindParent<T>(parentObject);
        }*/

        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static IList<DependencyProperty> GetAttachedProperties(DependencyObject obj)
        {
            List<DependencyProperty> result = new List<DependencyProperty>();

            foreach (PropertyDescriptor pd in TypeDescriptor.GetProperties(obj,
                new Attribute[] { new PropertyFilterAttribute(PropertyFilterOptions.All) }))
            {
                DependencyPropertyDescriptor dpd =
                    DependencyPropertyDescriptor.FromProperty(pd);

                if (dpd != null)
                {
                    result.Add(dpd.DependencyProperty);
                }
            }

            return result;
        }
    }
}
