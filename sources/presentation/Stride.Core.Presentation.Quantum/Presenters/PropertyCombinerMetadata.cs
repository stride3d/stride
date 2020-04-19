// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core;

namespace Stride.Core.Presentation.Quantum.Presenters
{
    /// <summary>
    /// Metadata for the property keys of <see cref="INodePresenter.AttachedProperties"/> that allows to define the
    /// way attached properties are combined for a view model.
    /// </summary>
    public class PropertyCombinerMetadata : PropertyKeyMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyCombinerMetadata"/> class.
        /// </summary>
        /// <param name="combiner">The method to use to combine values of the related attached property.</param>
        public PropertyCombinerMetadata(Func<IEnumerable<object>, object> combiner)
        {
            Combiner = combiner;
        }

        /// <summary>
        /// Gets the method that will combine multiple values of the same attached property into a single resulting value.
        /// </summary>
        public Func<IEnumerable<object>, object> Combiner { get; }
    }
}
