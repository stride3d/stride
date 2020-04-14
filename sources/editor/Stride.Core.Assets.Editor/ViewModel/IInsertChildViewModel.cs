// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core.Assets.Editor.View.Behaviors;
using Stride.Core.Annotations;

namespace Stride.Core.Assets.Editor.ViewModel
{
    /// <summary>
    /// An interface that represents an object that accepts to insert children before or after itself in the collection it is contained in, by drag and drop operations.
    /// </summary>
    public interface IInsertChildViewModel
    {
        /// <summary>
        /// Indicates whether this instance can insert the given children before or after itself.
        /// </summary>
        /// <param name="children">The children to insert.</param>
        /// <param name="position">The position to insert, before or after.</param>
        /// <param name="modifiers">The modifier keys currently active.</param>
        /// <param name="message">The feedback message that can be used in the user interface.</param>
        /// <returns><c>true</c> if this instance can insert the given children, <c>false</c> otherwise.</returns>
        bool CanInsertChildren([NotNull] IReadOnlyCollection<object> children, InsertPosition position, AddChildModifiers modifiers, [NotNull] out string message);

        /// <summary>
        /// Inserts the given children before or after itself in the collection it is contained in. Should be invoked only if <see cref="CanInsertChildren"/> returned <c>true</c>.
        /// </summary>
        /// <param name="children">The children to add.</param>
        /// <param name="position">The position to insert, before or after.</param>
        /// <param name="modifiers">The modifier keys currently active.</param>
        void InsertChildren([NotNull] IReadOnlyCollection<object> children, InsertPosition position, AddChildModifiers modifiers);
    }
}
