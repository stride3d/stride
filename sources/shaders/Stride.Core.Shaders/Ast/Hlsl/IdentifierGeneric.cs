// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System.Linq;

namespace Stride.Core.Shaders.Ast.Hlsl
{
    /// <summary>
    /// A generic identifier in the form Typename&lt;identifier1,..., identifiern&gt;
    /// </summary>
    public partial class IdentifierGeneric : CompositeIdentifier
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IdentifierGeneric"/> class.
        /// </summary>
        public IdentifierGeneric()
        {
            IsSpecialReference = true;
        }

        public IdentifierGeneric(string name, params Identifier[] composites)
            : this()
        {
            Text = name;
            Identifiers = composites.ToList();
        }

        /// <inheritdoc/>
        public override string Separator
        {
            get
            {
                return ",";
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return string.Format("{0}{1}", Text, Identifiers.Count == 0 ? string.Empty : base.ToString());
        }
    }
}
