// Copyright (c) Xenko contributors (https://xenko.com) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Xenko.Core.Shaders.Ast.Hlsl
{
    /// <summary>
    /// A C++ identifier with namespaces "::" separator
    /// </summary>
    public partial class IdentifierNs : CompositeIdentifier
    {
        /// <inheritdoc/>
        public override string Separator
        {
            get
            {
                return "::";
            }
        }
    }
}
