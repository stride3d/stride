// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;
using Xenko.Core.Assets.Editor.ViewModel;
using Xenko.Core.Annotations;
using Xenko.Core.Presentation.Quantum;

namespace Xenko.Core.Assets.Editor.Components.Properties
{
    /// <summary>
    /// Similar to <see cref="IAddChildViewModel"/> but for <see cref="IPropertyProviderViewModel"/>
    /// </summary>
    public interface IAddChildrenPropertiesProviderViewModel : IPropertyProviderViewModel
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
