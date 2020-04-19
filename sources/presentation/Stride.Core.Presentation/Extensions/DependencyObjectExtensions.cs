// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Reflection;
using System.Windows.Media;
using Stride.Core.Annotations;

namespace Stride.Core.Presentation.Extensions
{
    public static class DependencyObjectExtensions
    {
        /// <summary>
        /// Retrieves the public static DependencyProperties.
        /// </summary>
        /// <param name="source">DependencyObject that contains the DependencyProperties to be retrieved.</param>
        /// <param name="includingParentProperties">Indicates whether the DependencyProperties declared in the parent classes have to be retrieved too.</param>
        /// <returns>Returns an array of DependencyProperty owned by the DependencyObject.</returns>
        [NotNull]
        public static DependencyProperty[] GetDependencyProperties([NotNull] this DependencyObject source, bool includingParentProperties = false)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            // there is probably a better way using TypeDescriptor

            var dependencyPropertyType = typeof(DependencyProperty);

            var flags = BindingFlags.Public | BindingFlags.Static;
            if (includingParentProperties)
                flags |= BindingFlags.FlattenHierarchy;

            return source.DependencyObjectType.SystemType.GetFields(flags)
                .Where(fi => fi.MemberType == MemberTypes.Field && fi.FieldType == dependencyPropertyType)
                .Select(fi => (DependencyProperty)fi.GetValue(source))
                .OrderBy(dp => dp.Name)
                .ToArray();
        }

        /// <summary>
        /// Sets the value of a DependencyProperty on a DependencyObject and all its logical children.
        /// </summary>
        /// <param name="source">Root DependencyObject of which to set the DependencyProperty value.</param>
        /// <param name="property">DependencyProperty to set.</param>
        /// <param name="value">Value to set.</param>
        public static void DeepSetValue([NotNull] this DependencyObject source, [NotNull] DependencyProperty property, object value)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (property == null) throw new ArgumentNullException(nameof(property));

            source.SetValue(property, value);
            foreach (object child in LogicalTreeHelper.GetChildren(source as dynamic))
            {
                var depChild = child as DependencyObject;
                depChild?.DeepSetValue(property, value);
            }
        }

        /// <summary>
        /// Find the root parent, along the visual tree.
        /// </summary>
        /// <param name="source">Base node from where to start looking for root.</param>
        /// <returns>Returns the retrieved root, or null otherwise.</returns>
        [CanBeNull]
        public static Visual FindVisualRoot([NotNull] this DependencyObject source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            Visual root = null;
            while (source != null)
            {
                root = source as Visual;
                source = VisualTreeHelper.GetParent(source);
            }
            return root;
        }

        /// <summary>
        /// Find the first parent that match the given type, along the visual tree.
        /// </summary>
        /// <typeparam name="T">Type of parent to find.</typeparam>
        /// <param name="source">Base node from where to start looking for parent.</param>
        /// <returns>Returns the retrieved parent, or null otherwise.</returns>
        [CanBeNull]
        public static T FindVisualParentOfType<T>([NotNull] this DependencyObject source) where T : DependencyObject
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return FindParentOfType<T>(source, VisualTreeHelper.GetParent);
        }

        /// <summary>
        /// Find the first child that match the given type, along the visual tree.
        /// </summary>
        /// <typeparam name="T">Type of child to find.</typeparam>
        /// <param name="source">Base node from where to start looking for child.</param>
        /// <returns>Returns the retrieved child, or null otherwise.</returns>
        [CanBeNull]
        public static T FindVisualChildOfType<T>([NotNull] this DependencyObject source) where T : DependencyObject
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return FindChildOfType<T>(source, VisualTreeHelper.GetChildrenCount, VisualTreeHelper.GetChild);
        }

        /// <summary>
        /// Find all the children that match the given type, along the visual tree.
        /// </summary>
        /// <typeparam name="T">Type of child to find.</typeparam>
        /// <param name="source">Base node from where to start looking for children.</param>
        /// <returns>Returns the retrieved children, or empty otherwise.</returns>
        [ItemNotNull, NotNull]
        public static IEnumerable<T> FindVisualChildrenOfType<T>([NotNull] this DependencyObject source) where T : DependencyObject
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return FindChildrenOfType<T>(source, VisualTreeHelper.GetChildrenCount, VisualTreeHelper.GetChild);
        }

        /// <summary>
        /// Gets the first visual child of the given object.
        /// </summary>
        /// <param name="source">The object in which to look for a child.</param>
        /// <returns>The child if the given object has children, <c>null</c> otherwise.</returns>
        [CanBeNull]
        public static DependencyObject FindFirstVisualChild([NotNull] this DependencyObject source)
        {
            var childrenCount = VisualTreeHelper.GetChildrenCount(source);
            return childrenCount > 0 ? VisualTreeHelper.GetChild(source, 0) : null;
        }

        /// <summary>
        /// Find the first parent that match the given type, along the logical tree.
        /// </summary>
        /// <typeparam name="T">Type of parent to find.</typeparam>
        /// <param name="source">Base node from where to start looking for parent.</param>
        /// <returns>Returns the retrieved parent, or null otherwise.</returns>
        [CanBeNull]
        public static T FindLogicalParentOfType<T>([NotNull] this DependencyObject source) where T : DependencyObject
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return FindParentOfType<T>(source, LogicalTreeHelper.GetParent);
        }

        /// <summary>
        /// Finds the first child that match the given type, along the logical tree.
        /// Warning: this function is inefficient, use it for debug but not production.
        /// </summary>
        /// <typeparam name="T">Type of child to find.</typeparam>
        /// <param name="source">Base node from where to start looking for child.</param>
        /// <returns>Returns the retrieved child, or null otherwise.</returns>
        [CanBeNull]
        public static T FindLogicalChildOfType<T>([NotNull] this DependencyObject source) where T : DependencyObject
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return FindChildOfType<T>(source,
                d => LogicalTreeHelper.GetChildren(d).Cast<DependencyObject>().Count(),
                (d, i) => LogicalTreeHelper.GetChildren(d).Cast<DependencyObject>().ElementAt(i));
        }

        /// <summary>
        /// Finds all the children that match the given type, along the logical tree.
        /// Warning: this function is inefficient, use it for debug but not production.
        /// </summary>
        /// <typeparam name="T">Type of child to find.</typeparam>
        /// <param name="source">Base node from where to start looking for children.</param>
        /// <returns>Returns the retrieved children, or empty otherwise.</returns>
        [ItemNotNull, NotNull]
        public static IEnumerable<T> FindLogicalChildrenOfType<T>([NotNull] this DependencyObject source) where T : DependencyObject
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            return FindChildrenOfType<T>(source,
                d => LogicalTreeHelper.GetChildren(d).Cast<DependencyObject>().Count(),
                (d, i) => LogicalTreeHelper.GetChildren(d).Cast<DependencyObject>().ElementAt(i));
        }

        /// <summary>
        /// Checks that the given dependency object, retrieved as a template part of the calling object using <see cref="FrameworkElement.GetTemplateChild(string)"/>,
        /// exists and matches the given type.
        /// </summary>
        /// <typeparam name="T">The type expected for the template part.</typeparam>
        /// <param name="templatePart">The template part to evaluate.</param>
        /// <returns>The given template part, cast into the proper type.</returns>
        public static T CheckTemplatePart<T>(DependencyObject templatePart) where T : DependencyObject
        {
            if (templatePart == null)
                return null;

            if (templatePart is T == false)
            {
                throw new ArgumentException($"Invalid '{templatePart.GetType().FullName}' TemplatePart type. '{typeof(T).FullName}' expected.");
            }

            return (T)templatePart;
        }

        #region Helper methods

        /// <summary>
        /// Find the first parent that match the given type.
        /// </summary>
        /// <typeparam name="T">Type of parent to find.</typeparam>
        /// <param name="source">Base node from where to start looking for parent.</param>
        /// <param name="getParentFunc">Function that provide the parent element.</param>
        /// <returns>Returns the retrieved parent, or null otherwise.</returns>
        [CanBeNull]
        private static T FindParentOfType<T>(DependencyObject source, [NotNull] Func<DependencyObject, DependencyObject> getParentFunc) where T : DependencyObject
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
        /// Find the first child that match the given type.
        /// </summary>
        /// <typeparam name="T">Type of child to find.</typeparam>
        /// <param name="source">Base node from where to start looking for child.</param>
        /// <param name="getChildrenCountFunc">Function that provide the number of children in the current element.</param>
        /// <param name="getChildFunc">Function that provide a given child element by its index.</param>
        /// <returns>Returns the retrieved child, or null otherwise.</returns>
        [CanBeNull]
        private static T FindChildOfType<T>([NotNull] DependencyObject source, [NotNull] Func<DependencyObject, int> getChildrenCountFunc,
            [NotNull] Func<DependencyObject, int, DependencyObject> getChildFunc) where T : DependencyObject
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
                        return child as T;
                    child = FindChildOfType<T>(child, getChildrenCountFunc, getChildFunc);
                    if (child != null)
                        return (T)child;
                }
            }
            return null;
        }

        /// <summary>
        /// Find all the children that match the given type.
        /// </summary>
        /// <typeparam name="T">Type of child to find.</typeparam>
        /// <param name="source">Base node from where to start looking for children.</param>
        /// <param name="getChildrenCountFunc">Function that provide the number of children in the current element.</param>
        /// <param name="getChildFunc">Function that provide a given child element by its index.</param>
        /// <returns>Returns the retrieved children, empty otherwise.</returns>
        [ItemNotNull, NotNull]
        private static IEnumerable<T> FindChildrenOfType<T>([NotNull] DependencyObject source, [NotNull] Func<DependencyObject, int> getChildrenCountFunc,
            [NotNull] Func<DependencyObject, int, DependencyObject> getChildFunc) where T : DependencyObject
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (getChildrenCountFunc == null) throw new ArgumentNullException(nameof(getChildrenCountFunc));
            if (getChildFunc == null) throw new ArgumentNullException(nameof(getChildFunc));

            var childCount = getChildrenCountFunc(source);
            for (var i = 0; i < childCount; i++)
            {
                var child = getChildFunc(source, i);
                if (child == null)
                    continue;

                if (child is T)
                    yield return child as T;

                foreach (var subChild in FindChildrenOfType<T>(child, getChildrenCountFunc, getChildFunc))
                {
                    yield return subChild;
                }
            }
        }

        #endregion
    }
}
