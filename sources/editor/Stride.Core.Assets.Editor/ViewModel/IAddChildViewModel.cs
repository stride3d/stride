// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Stride.Core.Annotations;

namespace Stride.Core.Assets.Editor.ViewModel
{
    /// <summary>
    /// An interface that represents an object that accepts to add children to itself by drag and drop operations.
    /// </summary>
    public interface IAddChildViewModel
    {
        /// <summary>
        /// Indicates whether this instance can add the given children.
        /// </summary>
        /// <param name="children">The children to add.</param>
        /// <param name="modifiers">The modifier keys currently active.</param>
        /// <param name="message">The feedback message that can be used in the user interface.</param>
        /// <returns><c>true</c> if this instance can add the given children, <c>false</c> otherwise.</returns>
        bool CanAddChildren([NotNull] IReadOnlyCollection<object> children, AddChildModifiers modifiers, [NotNull] out string message);

        /// <summary>
        /// Adds the given children to this instance. Should be invoked only if <see cref="CanAddChildren"/> returned <c>true</c>.
        /// </summary>
        /// <param name="children">The children to add.</param>
        /// <param name="modifiers">The modifier keys currently active.</param>
        void AddChildren([NotNull] IReadOnlyCollection<object> children, AddChildModifiers modifiers);
    }
}
