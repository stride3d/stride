// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Collections.Generic;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// An interface used by generic definitions and instance.
    /// </summary>
    public interface IGenerics
    {
        /// <summary>
        /// Gets or sets the generic arguments.
        /// </summary>
        /// <value>
        /// The generic arguments.
        /// </value>
        List<TypeBase> GenericParameters { get; set; }

        /// <inheritdoc/>
        List<TypeBase> GenericArguments { get; set; }
    }
}
