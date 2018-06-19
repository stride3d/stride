// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Xenko.Core.Shaders.Ast
{
    /// <summary>
    /// A tag interface for an object referencing a type.
    /// </summary>
    public interface ITypeInferencer
    {
        /// <summary>
        /// Gets or sets the reference.
        /// </summary>
        /// <value>
        /// The reference.
        /// </value>
        TypeInference TypeInference { get; set; }
    }
}
