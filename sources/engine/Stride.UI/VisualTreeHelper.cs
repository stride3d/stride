// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Stride.Core.Extensions;

namespace Stride.UI
{
    public static class VisualTreeHelper
    {
        /// <summary>
        /// Find the first child that match the given type, along the visual tree.
        /// </summary>
        /// <typeparam name="T">Type of child to find.</typeparam>
        /// <param name="source">Base node from where to start looking for child.</param>
        /// <param name="name">(Optional) name of the element</param>
        /// <returns>Returns the retrieved child, or null otherwise.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static T FindVisualChildOfType<T>(this UIElement source, string name = null) where T : UIElement
        {
            var visualChildren = FindVisualChildrenOfType<T>(source);
            return name != null
                ? visualChildren.FirstOrDefault(e => string.Equals(e.Name, name, StringComparison.Ordinal))
                : visualChildren.FirstOrDefault();
        }

        /// <summary>
        /// Find all the children that match the given type, along the visual tree.
        /// </summary>
        /// <typeparam name="T">Type of child to find.</typeparam>
        /// <param name="source">Base node from where to start looking for children.</param>
        /// <returns>Returns the retrieved children, or empty otherwise.</returns>
        public static IEnumerable<T> FindVisualChildrenOfType<T>(this UIElement source) where T : UIElement
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return FindChildrenOfType<T>(source, e => e.VisualChildrenCollection.Count, (e, i) => e.VisualChildrenCollection[i]);
        }

        /// <summary>
        /// Find the first parent that match the given type, along the visual tree.
        /// </summary>
        /// <typeparam name="T">Type of parent to find.</typeparam>
        /// <param name="source">Base node from where to start looking for parent.</param>
        /// <returns>Returns the retrieved parent, or null otherwise.</returns>
        public static T FindVisualParentOfType<T>(this UIElement source) where T : UIElement
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return FindParentOfType<T>(source, e => e.VisualParent);
        }

        /// <summary>
        /// Find the root parent, along the visual tree.
        /// </summary>
        /// <param name="source">Base node from where to start looking for root.</param>
        /// <returns>Returns the retrieved root, or null otherwise.</returns>
        public static UIElement FindVisualRoot(this UIElement source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            UIElement root = null;
            while (source != null)
            {
                root = source;
                source = source.VisualParent;
            }
            return root;
        }

        /// <summary>
        /// Find the first parent that match the given type.
        /// </summary>
        /// <typeparam name="T">Type of parent to find.</typeparam>
        /// <param name="source">Base node from where to start looking for parent.</param>
        /// <param name="getParentFunc">Function that provide the parent element.</param>
        /// <returns>Returns the retrieved parent, or null otherwise.</returns>
        private static T FindParentOfType<T>(UIElement source, Func<UIElement, UIElement> getParentFunc) where T : UIElement
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (getParentFunc == null) throw new ArgumentNullException(nameof(getParentFunc));

            while (true)
            {
                // try to get visual parent
                var parent = getParentFunc(source);

                if (parent != null)
                {
                    if (parent is T)
                    {
                        // parent is of requested type, returned casted
                        return parent as T;
                    }

                    // there is a parent but not of request type, let's keep traversing the tree up
                    source = parent;
                    continue;
                }

                // failed to find visual parent
                return null;
            }
        }

        /// <summary>
        /// Find all the children that match the given type.
        /// </summary>
        /// <typeparam name="T">Type of child to find.</typeparam>
        /// <param name="source">Base node from where to start looking for children.</param>
        /// <param name="getChildrenCountFunc">Function that provide the number of children in the current element.</param>
        /// <param name="getChildFunc">Function that provide a given child element by its index.</param>
        /// <returns>Returns the retrieved children, empty otherwise.</returns>
        private static IEnumerable<T> FindChildrenOfType<T>(UIElement source, Func<UIElement, int> getChildrenCountFunc, Func<UIElement, int, UIElement> getChildFunc) where T : UIElement
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (getChildrenCountFunc == null) throw new ArgumentNullException(nameof(getChildrenCountFunc));
            if (getChildFunc == null) throw new ArgumentNullException(nameof(getChildFunc));

            var childCount = getChildrenCountFunc(source);
            for (var i = 0; i < childCount; i++)
            {
                var child = getChildFunc(source, i);
                if (child != null)
                {
                    if (child is T)
                        yield return child as T;

                    foreach (var subChild in FindChildrenOfType<T>(child, getChildrenCountFunc, getChildFunc).NotNull())
                    {
                        yield return subChild;
                    }
                }
            }
        }
    }
}
