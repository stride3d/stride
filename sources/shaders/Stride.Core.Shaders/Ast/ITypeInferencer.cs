// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
namespace Stride.Core.Shaders.Ast
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
