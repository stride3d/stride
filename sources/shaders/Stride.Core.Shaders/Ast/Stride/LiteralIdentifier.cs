// Copyright (c) Stride contributors (https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using Stride.Core.Shaders.Ast;

namespace Stride.Core.Shaders.Ast.Stride
{
    public partial class LiteralIdentifier : Identifier
    {
        public LiteralIdentifier()
        {
        }

        public LiteralIdentifier(Literal valueName)
            : base(valueName.ToString())
        {
            Value = valueName;
        }


        public Literal Value { get; set; }
    }
}
