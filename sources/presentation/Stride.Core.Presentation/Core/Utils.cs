// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using System.Text.RegularExpressions;
using Stride.Core;
using Stride.Core.Annotations;
using Stride.Core.Mathematics;
using Stride.Core.Reflection;

namespace Stride.Core.Presentation.Core
{
    /// <summary>
    /// A static class containing various useful methods and constants.
    /// </summary>
    public static class Utils
    {   
        /// <summary>
        /// An array of values that can be used for zooming.
        /// </summary>
        public static readonly double[] ZoomFactors = { 0.02, 0.05, 0.083, 0.125, 0.167, 0.20, 0.25, 0.333, 0.5, 0.667, 1.0, 1.5, 2.0, 3.0, 4.0, 5.0, 6.0, 8.0, 12.0, 16.0, 24.0 };

        /// <summary>
        /// The index of the factor <c>1.0</c> in the <see cref="ZoomFactors"/> array.
        /// </summary>
        public static readonly int ZoomFactorIdentityIndex = 10;

        [NotNull]
        public static string SplitCamelCase([NotNull] string input)
        {
            return Regex.Replace(input, "([a-z](?=[A-Z])|[A-Z](?=[A-Z][a-z]))", "$1 ");
        }

        public static Color4 GetUniqueColor(this Type type)
        {
            var displayAttribute = TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<DisplayAttribute>(type);
            var hash = displayAttribute?.Name.GetHashCode() ?? type.Name.GetHashCode();
            hash = hash >> 16 ^ hash;
            var hue = TypeDescriptorFactory.Default.AttributeRegistry.GetAttribute<DisplayAttribute>(type)?.CustomHue ?? hash % 360;
            return new ColorHSV(hue, 0.75f + (hash % 101) / 400f, 0.5f + (hash % 151) / 300f, 1).ToColor();
        }
    }
}
