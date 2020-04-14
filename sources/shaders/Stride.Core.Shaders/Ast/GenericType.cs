// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;
using System.Collections.Generic;
using Stride.Core;

namespace Stride.Core.Shaders.Ast
{
    /// <summary>
    /// Custom generic type.
    /// </summary>
    public partial class GenericType : GenericBaseType
    {
        public GenericType()
        {
        }

        public GenericType(string name, int parameterCount) : base(name, parameterCount)
        {
        }

        /// <inheritdoc/>
        [DataMember]
        public override List<Type> ParameterTypes { get; set; }

        /// <inheritdoc/>
        [DataMember]
        public override List<Node> Parameters { get; set; }
    }
}
