// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Xenko.Core.Annotations;
using Xenko.Core.Presentation.Quantum.Presenters;
using Xenko.Core.Presentation.Quantum.ViewModels;
using Xenko.Core.Quantum;

namespace Xenko.Core.Presentation.Quantum
{
    /// <summary>
    /// An interface representing an view model that can provide properties to build an <see cref="GraphViewModel"/>.
    /// </summary>
    public interface IPropertyProviderViewModel
    {
        /// <summary>
        /// Gets whether this view model is currently able to provide properties.
        /// </summary>
        bool CanProvidePropertiesViewModel { get; }

        /// <summary>
        /// Retrieves the root <see cref="IGraphNode"/> to use to generate properties.
        /// </summary>
        /// <returns>The root <see cref="IGraphNode"/> to use to generate properties.</returns>
        IObjectNode GetRootNode();

        /// <summary>
        /// Indicates whether the view model of a specific member should be constructed.
        /// </summary>
        /// <param name="member">The member to evaluate.</param>
        /// <returns><c>True</c> if the member node should be constructed, <c>False</c> otherwise.</returns>
        bool ShouldConstructMember([NotNull] IMemberNode member);

        /// <summary>
        /// Indicates whether the view model of a specific item of a collection should be constructed.
        /// </summary>
        /// <param name="collection">The collection to evaluate.</param>
        /// <param name="index">The index of the item to evaluate.</param>
        /// <returns><c>True</c> if the member node should be constructed, <c>False</c> otherwise.</returns>
        bool ShouldConstructItem([NotNull] IObjectNode collection, NodeIndex index);
    }
}
