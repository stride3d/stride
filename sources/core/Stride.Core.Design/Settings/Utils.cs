// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core.Annotations;

namespace Stride.Core.Settings
{
    // Extracted from Stride.Core.Presentation.Core.Utils. We could move it to somewhere public or shared later if needed.
    internal static class Utils
    {
        /// <summary>
        /// Updates the given field to the given value. If the field changes, invoke the given action.
        /// </summary>
        /// <typeparam name="T">The type of the field and the value.</typeparam>
        /// <param name="field">The field to update.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="action">The action to invoke if the field has changed.</param>
        public static void SetAndInvokeIfChanged<T>(ref T field, T value, [NotNull] Action action)
        {
            if (action == null) throw new ArgumentNullException(nameof(action));
            bool changed = !Equals(field, value);
            if (changed)
            {
                field = value;
                action();
            }
        }
    }
}
