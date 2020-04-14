// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using Stride.Core;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// A reference to a type.
    /// </summary>
    [DataContract]
    public class TypeInference
    {
        /// <summary>
        /// Gets or sets the declaration.
        /// </summary>
        /// <value>
        /// The declaration.
        /// </value>
        public IDeclaration Declaration { get; set; }

        /// <summary>
        /// Gets or sets the type.
        /// </summary>
        /// <value>
        /// The type.
        /// </value>
        public TypeBase TargetType { get; set; }

        /// <summary>
        /// Gets or sets the expected type.
        /// </summary>
        /// <value>
        /// The expected type.
        /// </value>
        public TypeBase ExpectedType { get; set; }

        /// <inheritdoc/>
        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
