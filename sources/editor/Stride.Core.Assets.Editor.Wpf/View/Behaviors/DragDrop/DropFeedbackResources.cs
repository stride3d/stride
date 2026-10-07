// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Windows;
using System.Windows.Media;

namespace Stride.Core.Assets.Editor.View.Behaviors
{
    /// <summary>
    /// Gives the theme resources that the drag and drop feedback draws with.
    /// </summary>
    /// <remarks>
    /// Every theme gives a value for each of these keys. A brush that the theme does not give is <c>null</c>, and the
    /// caller then keeps the default of the adorner. The two sizes have a default value, because an adorner must draw
    /// something when no theme is loaded, for example in a test or in a designer.
    /// </remarks>
    internal static class DropFeedbackResources
    {
        internal const string InsertLineBrushKey = "DropInsertLineBrush";
        internal const string InsertLineThicknessKey = "DropInsertLineThickness";
        internal const string InsertMarkerRadiusKey = "DropInsertMarkerRadius";
        internal const string TargetNeutralBrushKey = "DropTargetNeutralBrush";
        internal const string TargetAcceptBrushKey = "DropTargetAcceptBrush";
        internal const string TargetRefuseBrushKey = "DropTargetRefuseBrush";

        // These apply only when no theme is loaded. The themes give the values that the editor shows.
        private const double DefaultInsertLineThickness = 2.0;
        private const double DefaultInsertMarkerRadius = 3.0;

        internal static Brush GetInsertLineBrush(DependencyObject target)
        {
            return Find<Brush>(target, InsertLineBrushKey, SystemColors.HighlightBrush);
        }

        internal static double GetInsertLineThickness(DependencyObject target)
        {
            return Find(target, InsertLineThicknessKey, DefaultInsertLineThickness);
        }

        internal static double GetInsertMarkerRadius(DependencyObject target)
        {
            return Find(target, InsertMarkerRadiusKey, DefaultInsertMarkerRadius);
        }

        internal static Brush GetTargetNeutralBrush(DependencyObject target)
        {
            return Find<Brush>(target, TargetNeutralBrushKey, null);
        }

        internal static Brush GetTargetAcceptBrush(DependencyObject target)
        {
            return Find<Brush>(target, TargetAcceptBrushKey, null);
        }

        internal static Brush GetTargetRefuseBrush(DependencyObject target)
        {
            return Find<Brush>(target, TargetRefuseBrushKey, null);
        }

        /// <summary>
        /// Finds a resource of the given type for the given element.
        /// </summary>
        /// <typeparam name="T">The type of the resource.</typeparam>
        /// <param name="target">The element to search from, or <c>null</c> to search the application only.</param>
        /// <param name="key">The key of the resource.</param>
        /// <param name="defaultValue">The value to give when the resource is missing or of another type.</param>
        /// <returns>The resource, or <paramref name="defaultValue"/>.</returns>
        /// <remarks>
        /// The element is searched first, therefore a window or a control can override a value. A resource of another
        /// type is treated as a missing resource, because an adorner must not throw while it draws.
        /// </remarks>
        internal static T Find<T>(DependencyObject target, string key, T defaultValue)
        {
            var value = (target as FrameworkElement)?.TryFindResource(key) ?? Application.Current?.TryFindResource(key);
            return value is T result ? result : defaultValue;
        }
    }
}
